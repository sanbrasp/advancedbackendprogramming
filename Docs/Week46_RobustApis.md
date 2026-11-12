# Help Desk API Extended

Week 46 notes

---

## Overview
- [Week 44 (separate document)](../Docs/Week44_Introduction.md)
- [Week 45 (separate document)](../Docs/Week45_InfrastructureArchitecture.md)
- [Week 46](#week-46)
    - [Difference between validation errors and unexpected exceptions](#difference-between-validation-errors-and-unexpected-exceptions)
    - [Implement GlobalExceptionHandler with ProblemDetails and traceId](#implement-globalexceptionhandler-with-problemdetails-and-traceid)
    - [Configure Serilog from appsettings.json](#configure-serilog-from-appsettingsjson-with-console-and-file-sinks)
    - [Use structured logging](#use-structured-logging)
        - [Log levels](#log-levels)
    - [Explain what traceId is](#explain-what-traceid-is-and-why-it-is-important)
    - [Data Annotations on a DTO](#add-validation-with-data-annotations-to-a-dto)
    - [FluentValidation in a separate class](#implement-fluentvalidation-in-separate-class)
    - [Data Annotations vs FluentValidation](#justify-when-to-use-data-annotations-vs-fluentvalidation)
    - [Tests](#tests)
    - [Core tasks](#complete-at-least-three-of-the-core-tasks)
    - [Data leaks in logs](#identify-at-least-two-types-of-data-leaks-in-logs)

---

## Week 46

Learning goals for this week:
- [x] I can explain the difference between validation errors and unexpected exceptions in my own words
- [x] I can implement GlobalExceptionHandler with Problem Details and traceId
- [x] I can configure Serilog from appsettings.json with Console and File sinks
- [x] I can use structured logging (message templates) instead of string interpolation
- [x] I can explain what traceId is and why it is important for correlation
- [x] I can add validation with Data Annotations to a DTO
- [x] I can implement a FluentValidation validator in a separate class
- [x] I can justify when I would use Data Annotations vs. FluentValidation
- [x] I have completed at least three of the six core tasks
- [x] I can identify at least two types of data leaks that must not occur in logs

---

## Difference Between Validation Errors and Unexpected Exceptions

A **validation error** is the client's mistake: wrong or missing input. It is expected and predictable, so it gets a
`4xx` status. It is not an exception, and the exception handler is never involved. The response comes from
`[ApiController]` (Data Annotations) or from the action itself (FluentValidation). The client is told what to fix.

An **unexpected exception** is our mistake: a bug or a failure nobody planned for. It is thrown, and `GlobalExceptionHandler` 
catches it. The client gets a generic `500` with a `traceId`, and never the details. The details go to the log.

| Case | Example in Help Desk | Status | An exception? | Who produces the response |
|------|----------------------|--------|---------------|---------------------------|
| Validation error | POST with a too-long description | 400 | No | The action's result |
| Not found | GET `/api/tickets/99` | 404 | No | The action's result |
| Conflict | POST with an id that exists | 409 | No | The action's result |
| Unexpected exception | `/api/Diagnostics/throw` | 500 | Yes | The exception handler |
| Validation error | POST without a description | 400 | No | `[ApiController]`, before the action runs |

The two are treated differently because the client needs different things. After a validation error, it needs to know 
*what to change*. After an unexpected exception, there is nothing it can fix, and internal details could help an attacker.
So the client gets a `traceId`, and the details stay on the server.

[Handle errors in ASP.NET Core APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)

[🏠 To top](#overview)

---

## Implement GlobalExceptionHandler with ProblemDetails and traceId

Replaced `ErrorController` and `UseExceptionHandler("/error")` with `GlobalExceptionHandler`, a class implementing
`IExceptionHandler`.

**Baseline.** A temporary endpoint that throws (`GET /api/Diagnostics/throw`) gave a `500` with `application/problem+json`
and a body with `type`, `title`, `status` and `traceId`.

**Registration in `Program.cs`:**

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
```

`UseExceptionHandler()` without a path needs `AddProblemDetails()` as its fallback, otherwise the app throws at startup.
`ErrorController` was deleted.

**First version.** I built a `ProblemDetails` by hand and wrote it with `WriteAsJsonAsync`. It worked, but the response 
was poorer than the baseline:

| | Content type | `type` | `traceId` |
|---|---|---|---|
| Baseline (`ErrorController`) | `application/problem+json` | yes | yes |
| Hand-written with `WriteAsJsonAsync` | `application/json` | no | no |
| `IProblemDetailsService.TryWriteAsync` | `application/problem+json` | yes | yes |


```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred"
            }
        });
    }
}
```

**Update 11 Nov:** the handler now also takes `ILogger<GlobalExceptionHandler>` and logs the exception with the 
trace ID. See the traceId section.

Response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An unexpected error occurred",
  "status": 500,
  "traceId": "00-146c7a78ec94033b66f69ef5f1897b6e-c49b02695fbe55d0-00"
}
```

- `TryHandleAsync` returns `true` for "handled, and I wrote the whole response", and `false` to pass the exception on.
Returning `true` without writing anything gives a 404.
- I left `Detail` out on purpose. `exception.Message` can reveal internals to the client.
- The handler is registered in DI, so it can take services in its constructor. An endpoint like `ErrorController` 
could do that too, but a handler class is easier to swap in tests.
- `RequestTimingMiddleware` printed nothing for the failing request. The exception skips the code after
  `await _next(context)`, and the exception handler sits outside the timing middleware. This is a blind spot.
  I first wrote that task 2.4 would fix it, which was wrong. **Update 11 Nov:** task 3.2 covers it instead. The
  handler logs the exception itself, so the failure leaves an `ERR` entry. See the traceId section.
- RFC 7807 from the lecture has been replaced by RFC 9457. The members are the same.

[Handle errors in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)  
[RFC 9457: Problem Details for HTTP APIs](https://rfc-editor.org/rfc/rfc9457)

[🏠 To top](#overview)

---

## Configure Serilog From appsettings.json with Console and File sinks

Added the NuGet package `Serilog.AspNetCore` to `HelpDesk.Api`. The sinks are not hard-coded. They come from a top-level 
`Serilog` section in `appsettings.json`:

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
  "MinimumLevel": "Information",
  "WriteTo": [
    { "Name": "Console" },
    { "Name": "File", "Args": { "path": "Logs/log.txt" } }
  ]
},
```

`Using` is a list of sink packages. `WriteTo` is a list of sink objects, and each has a `Name`. The `File` sink also
needs `Args` with a `path`.

In `Program.cs` there are two loggers, as in the Serilog README:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
// ...
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
```

- The bootstrap logger covers start-up, before the configuration is read ("Starting up"). `CreateBootstrapLogger()` 
lets the final logger replace it.
- `AddSerilog()` with no arguments never reads `appsettings.json`, so only the Console sink would exist.
- Sinks are not set in code, so they can change in JSON without a rebuild.
- The old `Logging` section is not read by Serilog.
- `Logs/` is in `.gitignore`, because logs are runtime output and often contain request data.
- Serilog replaces the default logging pipeline, so `ILogger<T>` works everywhere as before. Only the destination
  and formatting change.
- `Program.cs` wraps start-up in try/catch/finally: `Log.Information("Starting up")`, `Log.Fatal` if start-up fails, 
and `Log.CloseAndFlush()` at the end.
- **Update (the JSON above is now outdated).** `MinimumLevel` became an object with `Default` and `Override`:

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
  "MinimumLevel": {
    "Default": "Information",
    "Override": { "Microsoft.AspNetCore": "Warning" }
  },
  "WriteTo": [
    { "Name": "Console" },
    { "Name": "File", "Args": { "path": "Logs/log.txt" } }
  ]
},
```

- `Override` `"Microsoft.AspNetCore": "Warning"` matches by category prefix. The framework's
  "Request starting / Route matched / Executing endpoint" lines disappeared, while my own `HelpDesk.Api...` lines
  stayed at Information.

Result: the same lines appear in the console and in `Logs/log.txt`. The file has a full date and time offset on each line, 
and it starts at "Now listening", because the bootstrap logger writes to the console only.

[Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore)  
[Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration)

[🏠 To top](#overview)

---

## Use Structured Logging

Replaced the `Console.WriteLine` calls in `RequestTimingMiddleware` and `AuditActionFilter` with `ILogger<T>` and
message templates.

My first version used string interpolation inside `LogInformation`:

```csharp
_logger.LogInformation($"{context.Request.Method} {context.Request.Path} ...");
```

That produces one finished string, so the values are not separate properties. A message template has named placeholders, 
and the values are passed as arguments:

```csharp
_logger.LogInformation("{Method} {Path} {ElapsedMs} ms",
    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);

_logger.LogInformation("Operation IDs: Singleton {Singleton}, Scoped {Scoped}, Transient {Transient}",
    _singletonOperation.OperationId, scopedOperation.OperationId, transientOperation.OperationId);
```

In the filter: `"Action {Action} starting"` and `"Action {Action} finished"`, with
`context.ActionDescriptor.DisplayName`.

```terminaloutput
[19:00:57 INF] Action HelpDesk.Api.Controllers.TicketsController.GetAll (HelpDesk.Api) starting
[19:00:57 INF] Action HelpDesk.Api.Controllers.TicketsController.GetAll (HelpDesk.Api) finished
[19:00:57 INF] GET /api/Tickets 83 ms
[19:00:57 INF] Operation IDs: Singleton 5960, Scoped 1470, Transient 8565
```

- Placeholders are matched by position, so the arguments must be in the same order as the placeholders. Names use
  PascalCase.
- With a template, `Method` and `ElapsedMs` stay separate named properties, so they can be searched and filtered.
A JSON formatter such as `RenderedCompactJsonFormatter` shows them (mentioned in the Serilog README).
- I left the time out of the filter message, because Serilog stamps every line already.
- `ILogger<T>` is registered as Singleton ("Framework-provided services" in the DI docs). That is why it is safe in the 
middleware's constructor, which is built once at startup.
- `ILogger<T>` goes in the **constructor** only. An interface method like `OnActionExecuted` has a fixed signature, 
so extra parameters give a compile error. Only the convention-based `InvokeAsync` of middleware can take extra services.
- Rider showed CA1873 ("evaluation of this argument may be expensive"). It is a suggestion. 
The arguments are plain property reads, so I left it, as the docs allow.
- `ILogger<T>`: T is used as the log category, so each line shows which class wrote it.

### Log levels

| Event | Level | Reason |
|---|---|---|
| Unexpected exception (500) | Error | The request's execution was stopped by a failure and a developer must look at it. Not Critical, because the application as a whole keeps running. |
| Validation failure (400) | Warning | The client sent bad data. Unusual, but it is not a fault in the server. |
| Normal request timing line | Information | Normal operation. |

[Logging in .NET and ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0)  
[Dependency injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)  
[CA1873: Avoid potentially expensive logging](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1873)  
[LogLevel Enum](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.loglevel)

[🏠 To top](#overview)

---

## Explain what `traceId` is and why it is important

A `traceId` is a unique ID for one request. The client gets it in the Problem Details response, and the same value is 
written to the log. A client can quote it, and I can find the exact log entry with the exception and stack trace.

**Logging the exception (task 3.2).** `GlobalExceptionHandler` now takes `ILogger<GlobalExceptionHandler>` 
in its constructor and logs before it writes the response:

```csharp
_logger.LogError(exception, "Error has occurred. Refer to this trace ID: {TraceId}",
    Activity.Current?.Id ?? httpContext.TraceIdentifier);
```

- The exception is the **first** argument. That makes Serilog write the stack trace under the line.
- The level is Error, because the request was stopped by a failure.

**First version did not match (task 3.3).** I first logged `httpContext.TraceIdentifier`:

| Where | Value | What it is |
|---|---|---|
| Log | `0HNQ021SEVDIO:00000003` | `httpContext.TraceIdentifier`, the server's own request ID |
| Response | `00-6109740f51eaf490dac16d77680d6035-17c6e8b3589f22c6-00` | `Activity.Current.Id`, the W3C trace ID |

The framework sets the response value with `Activity.Current?.Id ?? httpContext?.TraceIdentifier`, so the log must
use the same expression. The W3C format is version, trace ID, parent ID and flags, and it is made to follow a request
across several systems.

**Result.** Log and response now match:

```terminaloutput
[17:34:59 ERR] Error has occurred. Refer to this trace ID: 00-ef9ed4d20e9596507e06b3c297e7555b-80c6db1c83a95dff-00
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An unexpected error occurred",
  "status": 500,
  "traceId": "00-ef9ed4d20e9596507e06b3c297e7555b-80c6db1c83a95dff-00"
}
```

- **Why search by ID and not timestamps:** many requests can share the same timestamp, but each has one unique trace ID.
- **The client does not see the stack trace.** The response body has only four fields. The stack trace goes to 
  the console and `Logs/log.txt` only.
- **Reading the stack trace:** the first `at` line is where the exception was thrown (`DiagnosticsController.cs:line 35`). 
  Lines with a file path are my code, the rest is framework code.
- `RequestTimingMiddleware` still prints no timing line for the failing request, but the failure now leaves an
  `ERR` entry.

[DefaultProblemDetailsFactory.cs (ASP.NET Core source)](https://source.dot.net/Microsoft.AspNetCore.Mvc.Core/Infrastructure/DefaultProblemDetailsFactory.cs.html)  
[W3C Trace Context](https://www.w3.org/TR/trace-context/)

[🏠 To top](#overview)

---

## Add Validation with Data Annotations to a DTO

`Create` used to take `TicketRecord` straight from the client. It now takes `CreateTicketRequestDto` 
(in `HelpDesk.Api/DTOs`) and maps it to a `TicketRecord` inside the action. The DTO separates what the client may send 
from the Core model, and the validation rules live in the Api project.

- The DTO must be `public`, because a public controller action cannot take an internal type as a parameter.
- `[Required]` does nothing on `int` and `DateTime`. A non-nullable value type always has a value, so a missing `Id` 
  becomes `0` and passes. `[Range(1, int.MaxValue)]` catches it instead.
- `[Required]` on a `string` works: a missing or empty value fails.
- `[ApiController]` checks the annotations before the action runs. If one fails, it returns the 400 itself, and
  `Create` is never called.

**Test request:**  
`POST /api/tickets` with a JSON body where the `description` field is left out. `[Required]` on
`Description` fails, and this is the response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Description": [
      "The Description field is required."
    ]
  },
  "traceId": "00-ba78ab0326bef8d939a5df795e038aaf-998e7e065afcc22f-00"
}
```

[Model validation in ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0)

[🏠 To top](#overview)

---

## Implement FluentValidation in separate class

Data Annotations need compile-time constants, so `[StringLength(500)]` cannot read `MaxDescriptionLength` from the
settings. A FluentValidation validator can, because it gets `IOptions<TicketSettings>` by constructor injection.

Packages: `FluentValidation` and `FluentValidation.DependencyInjectionExtensions`.

```csharp
internal class CreateTicketRequestDtoValidator : AbstractValidator<CreateTicketRequestDto>
{
    public CreateTicketRequestDtoValidator(IOptions<TicketSettings> settings)
    {
        RuleFor(request => request.Description).MaximumLength(settings.Value.MaxDescriptionLength);
        RuleFor(request => request.DateOfDeadline).GreaterThan(request => request.DateOfIssue);
    }
}
```

Registration in `Program.cs` (service type first, implementation second):

```csharp
builder.Services.AddScoped<IValidator<CreateTicketRequestDto>, CreateTicketRequestDtoValidator>();
```

In `Create`, the controller calls the validator manually and copies the errors into `ModelState`:

```csharp
var validationResult = _validator.Validate(ticketRequestDto);
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
    }

    return ValidationProblem(ModelState);
}
```

- This replaced the manual length check in `Create`.
- `StringLength(500)` was removed from the DTO, so the limit lives in one place, the settings.
- `ValidationProblem(ModelState)` gives the same 400 shape as Data Annotations, so the client sees one format.

**Test request:** `POST /api/tickets` with a 553-character description. The limit in the settings is 500, so the
validator fails, and this is the response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Description": [
      "The length of 'Description' must be 500 characters or fewer. You entered 553 characters."
    ]
  },
  "traceId": "00-7b3066e8bfe18127e90892e023c334da-66b18c7c4c44c6e5-00"
}
```

**Rule across two properties (12 Nov).** The deadline must be later than the issue date. A built-in attribute looks
at one property, so this rule is in the validator. The rule does not know about the 14-day default. That default
lives in the DTO and only applies when the client leaves the date out. The rule compares whatever dates the DTO has.

**Test request:**  
`POST /api/tickets` with `dateOfIssue` `2026-11-12T10:00:00Z` and `dateOfDeadline`
`2026-11-10T10:00:00Z`. The response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "DateOfDeadline": [
      "'Date Of Deadline' must be greater than '12/11/2026 10:00:00'."
    ]
  },
  "traceId": "00-e06553d6c2b4ae4b76fec5ff47d8bf53-f5059dcb661008f3-00"
}
```

- A POST with a JSON body needs the header `Content-Type: application/json`. Without it the API answers `415`
  (Unsupported Media Type).

[FluentValidation: ASP.NET Core](https://docs.fluentvalidation.net/en/latest/aspnet.html)  
[FluentValidation: Built-in Validators](https://docs.fluentvalidation.net/en/latest/built-in-validators.html)

[🏠 To top](#overview)

---

## Justify when to use Data Annotations vs FluentValidation

| Use | When |
|---|---|
| Data Annotations | Simple, fixed rules on one property (required, length, range). They run automatically and need no extra code. |
| FluentValidation | The rule needs a value from settings or a service, compares several properties, or is conditional. The rules live in their own class, which can be tested on its own. |

In this project: `[Required]` and `[Range]` are Data Annotations. The description length is FluentValidation, 
because the limit comes from `TicketSettings`.

**Reasoning:**

- **Data Annotations first.** The rule sits on the property it belongs to, and `[ApiController]` runs it
  automatically, so it is the least code. The limit is that an attribute only takes compile-time constants, and the
  built-in attributes look at one property at a time.
- **FluentValidation when an attribute cannot express the rule.** The validator is a normal class created by DI, so
  it can take `IOptions<TicketSettings>` or other services. The rules are code, so they can compare properties or
  depend on a condition. The cost is an extra package, an extra class, a registration, and a manual call in the
  action.
- **One place per rule.** The description length is checked only in the validator, not also by an attribute or by
  hand in the controller.

[🏠 To top](#overview)

---

## Tests

`HelpDesk.Tests` is an xUnit project that references `HelpDesk.Api` and uses the package
`Microsoft.AspNetCore.Mvc.Testing`. The package's major version must match the target framework: version 11.0.0
failed to install on `net10.0`, and a 10.x version worked.

**Starting the app from a test.**  
`HelpDeskApiFactory` inherits `WebApplicationFactory<Program>` and starts the API in
memory. The real API key is in user secrets, and a test must not depend on that, so the factory supplies its own key:

```csharp
public class HelpDeskApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:ApiKey"] = "test-key"
            });
        });
    }
}
```

- The tests send `X-Api-Key: test-key` and get through, so the in-memory value is the one in use.
- `context` is not used. The lambda must take two parameters, and an unused one can be written as `_`.
- `TicketsApiTests` implements `IClassFixture<HelpDeskApiFactory>`, so xUnit creates one factory and shares it between
  the tests. The test class and its constructor must be `public`, because xUnit creates the class itself.

**The three tests:**

| Test | Request | Checks |
|---|---|---|
| `GetAll_WithApiKey_ReturnsOk` | GET `/api/tickets` | 200 |
| `Create_WithoutDescription_ReturnsBadRequestWithErrors` | POST without a description | 400, and the body has an `errors` field |
| `GetAll_WhenRepositoryThrows_ReturnsProblemDetailsWithoutDetails` | GET `/api/tickets` with a throwing repository | 500, `application/problem+json`, a `traceId`, no exception message, no stack trace |

**Forcing a 500.** `ThrowingTicketRepository` (in `HelpDesk.Tests/Models`) implements `ITicketRepository`, and every
method throws `new Exception("Secret database details")`. The third test swaps it in for that test only:

```csharp
var client = _factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureTestServices(services =>
    {
        services.AddSingleton<ITicketRepository, ThrowingTicketRepository>();
    });
}).CreateClient();
```

- This works because `TicketService` depends on the `ITicketRepository` interface, not on the concrete class.
- The class is a **stub**: a controllable replacement for a dependency. A **mock** is a stand-in the test asserts
  against, usually built with a library like Moq. My asserts check the HTTP response, so a hand-written stub is enough.
- The test asserts that the body does not contain `Secret database details` or `ThrowingTicketRepository`. That
  proves the exception message and the stack trace stay on the server.

**Mistakes I made on the way:**

- The first test got a 404, because I requested `/api/HelpDesk/Tickets`. The route is `/api/tickets`.
- `Assert.Contains` takes the text to look for first, and the text to search in second. I had them swapped.
- Searching for `errors` without quotes would also match the title "One or more validation errors occurred.", so the
  test looks for `"\"errors\""`, the JSON field name.

**Result:** all three tests pass (12 Nov).

[Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)  
[Best practices for writing unit tests - .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)  
[Anonymous types - C#](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/anonymous-types)

[🏠 To top](#overview)

---

## Complete at least three of the core tasks

The six core tasks are written for a new order API. I did them inside Help Desk instead, so some details differ.
Tasks 1 and 5 were answered with help from Claude.

| # | Task | Status | Where, and what differs |
|---|---|---|---|
| 1 | Classify error types | Done | Table below |
| 2 | Global exception handler | Done, with a deviation | The GlobalExceptionHandler and traceId sections. My handler maps every exception to 500. The task also maps `ArgumentNullException` to 400. I use controllers, not Minimal API |
| 3 | Configure Serilog | Done, with deviations | The Serilog and structured logging sections. Not done: Debug level in Development, daily rolling file, `WithMachineName` |
| 4 | Validation with both techniques | Done | The Data Annotations, FluentValidation and Justify sections. My DTO combines the two techniques |
| 5 | Logs that do not leak data | Done | Below |
| 6 | Test all three scenarios | Done | The Tests section. The tests are named `Method_When_Condition_Should_Expected` |

### Task 1: classify error types

| # | Situation | Type | Status | Reason |
|---|---|---|---|---|
| A | The client sends `amount: -10` | Validation error | 400 | The input breaks a rule for the field, and the client can fix it. |
| B | Order 99 does not exist in the database | Domain error | 404 | The request is valid, but the resource does not exist. An expected situation. |
| C | The database connection drops in the middle of a call | Unexpected exception | 500 | A failure on the server that the client cannot fix. The details go to the log only. |
| D | The JSON is missing a `[Required]` field | Validation error | 400 | Required input is missing, and the client can fix it. |
| E | The user tries to delete another user's order | Domain error | 403 | The input is valid and the order exists, but this user is not allowed to do it. |

### Task 5: logs that do not leak data

The code from the task:

```csharp
_logger.LogInformation($"Ny bruker registrert: {user.Email}, passord: {user.Password}");
_logger.LogDebug("Hele request-objektet: {@request}");
_logger.LogError(ex, $"Databasefeil: {connectionString}");
```

| Line | Leak | Risk |
|---|---|---|
| 1 | The password in clear text, and the email | Anyone who can read the log can log in as the user. The email is personal data. |
| 2 | The whole request object (`{@request}` writes out every property) | Every field ends up in the log, including passwords, tokens and personal data. |
| 3 | The connection string | It holds the server, user name and password, which gives direct access to the database. |

Lines 1 and 3 also use string interpolation, so the log loses its structure.

Improved version (assuming the user has an `Id`):

```csharp
// An ID instead of the email, and no password. Information: a normal business event.
_logger.LogInformation("New user registered: {UserId}", user.Id);

// One chosen, harmless value instead of the whole object. Debug: detail for developers only.
_logger.LogDebug("Registration request received for user {UserId}", user.Id);

// The exception first, so the stack trace is logged. No connection string. Error: the operation failed.
_logger.LogError(ex, "Database error while registering user {UserId}", user.Id);
```

**Why passwords and connection strings must not be logged:**

- **Technical:**  
  logs are plain text. They are kept for a long time, copied to other systems, and read by many more
  people than the database is. A password or connection string in a log is a working credential for anyone with log
  access.
- **Legal:**  
  GDPR Article 5(1)(c) requires personal data to be "limited to what is necessary" (data minimisation).
  Article 5(1)(f) requires "appropriate security of the personal data", and Article 32 requires "appropriate
  technical and organisational measures". Logging emails and passwords breaks all three.

[Art. 5 GDPR](https://gdpr-info.eu/art-5-gdpr/)  
[Art. 32 GDPR](https://gdpr-info.eu/art-32-gdpr/)

[🏠 To top](#overview)

---

## Identify at least two types of data leaks in logs

Two types of data that must never end up in logs: **secrets** (API keys) and **personal data** 
(emails, and free-text fields such as `User` and `Description`).

- The API key is not in the log. My log lines only contain method, path, elapsed time, operation IDs and action name, 
and nothing logs headers or request bodies.
- API keys and other secrets, emails, and personal data in free-text fields (`User`, `Description`).
- For a ticket, log the ticket ID, not the description. The ID is enough to find the ticket, and free text can contain 
anything the user types.

[🏠 To top](#overview)


---