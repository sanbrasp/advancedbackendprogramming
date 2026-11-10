# Help Desk API Extended

Week 46 notes

---

## Overview
- [Week 44 (separate document)](Week44_Introduction.md)
- [Week 45 (separate document)](../Docs/Week45_InfrastructureArchitecture.md)
- [Week 46](#week-46)

---

## Week 46

Learning goals for this week:  
- [ ] I can explain the difference between validation errors and unexpected exceptions in my own words
- [ ] I can implement GlobalExceptionHandler with Problem Details and traceId
- [ ] I can configure Serilog from appsettings.json with Console and File sinks
- [ ] I can use structured logging (message templates) instead of string interpolation
- [ ] I can explain what traceId is and why it is important for correlation
- [ ] I can add validation with Data Annotations to a DTO
- [ ] I can implement a FluentValidation validator in a separate class
- [ ] I can justify when I would use Data Annotations vs. FluentValidation
- [ ] I have completed at least three of the six core tasks
- [ ] I can identify at least two types of data leaks that must not occur in logs

---

## Difference Between Validation Errors and Unexpected Exceptions

A **validation error** is the client's mistake: wrong or missing input. It is expected and predictable, so it gets a `4xx` status. It is not an exception. The action returns the result itself, and the exception handler is never involved. The client is told what to fix.

An **unexpected exception** is our mistake: a bug or a failure nobody planned for. It is thrown, and `GlobalExceptionHandler` catches it. The client gets a generic `500` with a `traceId`, and never the details. The details go to the log.

| Case | Example in Help Desk | Status | An exception? | Who produces the response |
|------|----------------------|--------|---------------|---------------------------|
| Validation error | POST with a too-long description | 400 | No | The action's result |
| Not found | GET `/api/tickets/99` | 404 | No | The action's result |
| Conflict | POST with an id that exists | 409 | No | The action's result |
| Unexpected exception | `/api/Diagnostics/throw` | 500 | Yes | The exception handler |

The two are treated differently because the client needs different things. After a validation error, it needs to know *what to change*. After an unexpected exception, there is nothing it can fix, and internal details could help an attacker. So the client gets a `traceId`, and the details stay on the server.

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

`UseExceptionHandler()` without a path needs `AddProblemDetails()` as its fallback, otherwise the app throws at startup. `ErrorController` was deleted.

**First version.** I built a `ProblemDetails` by hand and wrote it with `WriteAsJsonAsync`. It worked, but the response was poorer than the baseline:

| | Content type | `type` | `traceId` |
|---|---|---|---|
| Baseline (`ErrorController`) | `application/problem+json` | yes | yes |
| Hand-written with `WriteAsJsonAsync` | `application/json` | no | no |
| `IProblemDetailsService.TryWriteAsync` | `application/problem+json` | yes | yes |

**Final version.** The handler gets `IProblemDetailsService` by constructor injection, sets the status code and writes through `TryWriteAsync`. The framework then builds the response the standard way.

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

Response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An unexpected error occurred",
  "status": 500,
  "traceId": "00-146c7a78ec94033b66f69ef5f1897b6e-c49b02695fbe55d0-00"
}
```

- `TryHandleAsync` returns `true` for "handled, and I wrote the whole response", and `false` to pass the exception on. Returning `true` without writing anything gives a 404.
- I left `Detail` out on purpose. `exception.Message` can reveal internals to the client.
- The handler is registered in DI, so it can take services in its constructor. An endpoint like `ErrorController` could do that too, but a handler class is easier to swap in tests.
- `RequestTimingMiddleware` printed nothing for the failing request. The exception skips the code after `await _next(context)`, and the exception handler sits outside the timing middleware. This is a blind spot, which task 2.4 fixes.
- RFC 7807 from the lecture has been replaced by RFC 9457. The members are the same.

[Handle errors in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)  
[RFC 9457: Problem Details for HTTP APIs](https://rfc-editor.org/rfc/rfc9457)

[🏠 To top](#overview)

---

## Configure Serilog From appsettings.json with Console and File sinks

---

## Use Structured Logging

---

## Explain what `traceId` is and why it is important

---

## Add Validation with Data Annotations to a DTO

---

## Implement FluentValidation in separate class

---

## Justify when to use Data Annotations vs FluentValidation

---

## Complete at least three of the core tasks

---

## Identify at least two types of data leaks in logs

---

