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
    - [Core tasks](#complete-at-least-three-of-the-core-tasks)
    - [Data leaks in logs](#identify-at-least-two-types-of-data-leaks-in-logs)

---

## Week 46

Learning goals for this week:
- [ ] I can explain the difference between validation errors and unexpected exceptions in my own words
- [x] I can implement GlobalExceptionHandler with Problem Details and traceId
- [x] I can configure Serilog from appsettings.json with Console and File sinks
- [x] I can use structured logging (message templates) instead of string interpolation
- [x] I can explain what traceId is and why it is important for correlation
- [x] I can add validation with Data Annotations to a DTO
- [x] I can implement a FluentValidation validator in a separate class
- [x] I can justify when I would use Data Annotations vs. FluentValidation
- [ ] I have completed at least three of the six core tasks
- [x] I can identify at least two types of data leaks that must not occur in logs

---

## Difference Between Validation Errors and Unexpected Exceptions

A **validation error** is the client's mistake: wrong or missing input. It is expected and predictable, so it gets a 
`4xx` status. It is not an exception. The action returns the result itself, and the exception handler is never involved. 
The client is told what to fix.

An **unexpected exception** is our mistake: a bug or a failure nobody planned for. It is thrown, and `GlobalExceptionHandler` 
catches it. The client gets a generic `500` with a `traceId`, and never the details. The details go to the log.

| Case | Example in Help Desk | Status | An exception? | Who produces the response |
|------|----------------------|--------|---------------|---------------------------|
| Validation error | POST with a too-long description | 400 | No | The action's result |
| Not found | GET `/api/tickets/99` | 404 | No | The action's result |
| Conflict | POST with an id that exists | 409 | No | The action's result |
| Unexpected exception | `/api/Diagnostics/throw` | 500 | Yes | The exception handler |

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

**Final version.** The handler gets `IProblemDetailsService` by constructor injection, sets the status code and writes
through `TryWriteAsync`. The framework then builds the response the standard way.

```csharp
internal class GlobalExceptionHandler : IExceptionHandler
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
- `RequestTimingMiddleware` printed nothing for the failing request. The exception skips the code after `await _next(context)`, 
and the exception handler sits outside the timing middleware. 
  This is a blind spot, which task 2.4 fixes. **Correction:** task 2.4 does not fix this. The planned fix is task 3.2: 
the handler logs the exception itself.  
  **Update 11 Nov:** done in task 3.2, see the traceId section.
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
- Serilog replaces the default logging pipeline, so `ILogger<T>` works everywhere as before. Only the destination and formatting change.
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
"Request starting / Route matched / Executing endpoint" lines disappeared, while my own `HelpDesk.Api...` lines stayed at Information.

Result: the same lines appear in the console and in `Logs/log.txt`. The file has a full date and time offset on each line, 
and it starts at "Now listening", because the bootstrap logger writes to the console only.

[Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore)  
[Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration)

[🏠 To top](#overview)

---

## Use Structured Logging

Replaced the `Console.WriteLine` calls in `RequestTimingMiddleware` and `AuditActionFilter` with `ILogger<T>` and message templates.

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

In the filter: `"Action {Action} starting"` and `"Action {Action} finished"`, with `context.ActionDescriptor.DisplayName`.

```terminaloutput
[19:00:57 INF] Action HelpDesk.Api.Controllers.TicketsController.GetAll (HelpDesk.Api) starting
[19:00:57 INF] Action HelpDesk.Api.Controllers.TicketsController.GetAll (HelpDesk.Api) finished
[19:00:57 INF] GET /api/Tickets 83 ms
[19:00:57 INF] Operation IDs: Singleton 5960, Scoped 1470, Transient 8565
```

- Placeholders are matched by position, so the arguments must be in the same order as the placeholders. Names use PascalCase.
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

The framework sets the response value with `Activity.Current?.Id ?? httpContext?.TraceIdentifier`, 
so the log must use the same expression. The W3C format is version, trace ID, parent ID and flags, and it is made to follow a request across several systems.

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

- **Why not timestamps:** many requests can share the same timestamp, but each has one unique trace ID.
- **The client does not see the stack trace.** The response body has only four fields. The stack trace goes to 
  the console and `Logs/log.txt` only.
- **Reading the stack trace:** the first `at` line is where the exception was thrown (`DiagnosticsController.cs:line 35`). 
  Lines with a file path are my code, the rest is framework code.
- `RequestTimingMiddleware` still prints no timing line for the failing request, but the failure now leaves an `ERR` entry.

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
- `[ApiController]` checks the annotations before the action runs and returns the 400 itself. `Create` is never called.

POST without a description:

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

---

## Justify when to use Data Annotations vs FluentValidation

| Use | When |
|---|---|
| Data Annotations | Simple, fixed rules on one property (required, length, range). They run automatically and need no extra code. |
| FluentValidation | The rule needs a value from settings or a service, compares several properties, or is conditional. The rules live in their own class, which can be tested on its own. |

In this project: `[Required]` and `[Range]` are Data Annotations. The description length is FluentValidation, 
because the limit comes from `TicketSettings`.

[🏠 To top](#overview)

---

## Complete at least three of the core tasks

---

## Identify at least two types of data leaks in logs

Two types of data that must never end up in logs: **secrets** (API keys) and **personal data** 
(emails, and free-text fields such as `User` and `Description`).

- The API key is not in the log. My log lines only contain method, path, elapsed time, operation IDs and action name, 
and nothing logs headers or request bodies.
- Never log: API keys and other secrets, emails, and personal data in free-text fields (`User`, `Description`).
- For a ticket, log the ticket ID, not the description. The ID is enough to find the ticket, and free text can contain 
anything the user types.

[🏠 To top](#overview)


---