# Help Desk API

Week 45 notes

---

## Overview
- [Week 44 (separate document)](Week44_Introduction.md)
- [Week 45](#week-45)
  - [Explain the Request Pipeline](#explain-request-pipeline-and-middleware-order)
  - [Short-circuiting and the next delegate](#short-circuiting-and-correct-use-of-the-next-delegate)
  - [Custom Middleware Class](#custom-middleware-class)
  - [Distinguish Between Middleware and Filters](#distinguish-between-middleware-and-filters)
  - [Register Services in the DI Container With the Right Lifetime](#register-services-in-di-container-with-right-lifetime)
  - [Use Constructor Injection](#constructor-injection-and-why-service-locator-is-anti-pattern)
  - [Set up the Options Pattern with Validation](#set-up-options-pattern-with-validation)
  - [Explain Why Secrets Don't Belong in appsettings.json](#why-secrets-dont-belong-in-appsettingsjson)
  - [Describe Responsibility, Dependency Diagram](#responsibility-of-each-layer--dependency-diagram)
  - [Compare Technical Layering and Feature-Based Structure](#compare-technical-layering-and-feature-based-structure)
- [Sources](#sources)

---

## Week 45

Learning goals for week 45:

Goals for this project:
- [ ] 1 I can explain the request pipeline and what the order of middleware means
- [ ] 2 I can explain short-circuiting and the correct use of the next delegate
- [ ] 3 I can write a custom middleware class
- [ ] 4 I can distinguish between middleware and filters and explain their use cases
- [ ] 5 I can register services in the DI container with the right lifetime
- [ ] 6 I can use constructor injection and explain why Service Locator is an anti-pattern
- [ ] 7 I can set up the Options pattern with validation and use IOptions in a controller
- [ ] 8 I can explain why secrets don't belong in appsettings.json
- [ ] 9 I can describe the responsibility of each layer and draw a dependency diagram
- [ ] 10 I can compare technical layering and feature-based structure

---

### Explain Request Pipeline and Middleware Order

The request passes through the middleware in the order they are registered in `Program.cs`.  
Then the response travels back in the reverse order. 

The exception handler should always be registered first of all middleware.  

```csharp
app.UseExceptionHandler("/error");
```

When it is first, it wraps everything that comes after it and can catch any exceptions from them.  
It can't catch exceptions from middleware that is registered before it.


[Handle errors in ASP.NET Core (Microsoft)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)

---

### Short Circuiting And Correct Use of the Next Delegate

Tested in `Program.cs`: 

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine($"A in {context.Request.Path}");
    
    await next.Invoke(context);
    
    Console.WriteLine("A out");
});
app.Use(async (context, next) =>
{
    Console.WriteLine($"B in {context.Request.Path}");

    await next.Invoke(context);

    Console.WriteLine("B out");
});
```

Output:

```text
A in /api/tickets
B in /api/tickets
B out
A out
A in /favicon.ico
B in /favicon.ico
B out
A out
```

- When A reaches `await next(...)`, it pauses and waits.  
- B runs inside that wait. Then B pauses when it reaches `await next(...)`, while the controller runs.  
- When the controller finishes, the code resumes in reverse:  
- B continues after its `next` (B out)., and only then can A continue (A out).

---

### Custom Middleware Class

---

### Distinguish Between Middleware and Filters

---

### Register Services in DI Container With Right Lifetime

The ticket store is registered as a `Singleton`.  

```csharp
builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
```

| Lifetime | Meaning |
|----------|---------|
| Transient | A new instance for every request |
| Scoped | One instance per circuit |
| Singleton | One instance shared by every request for the app's lifetime|

Singleton chosen because the data needs to live inside the repo instance. Transient or scoped would 
create a new, empty store every time, and the tickets disappear.  
Because every request shares the one instance, it uses a `ConcurrentDictionary`.

---

### Constructor Injection and Why Service Locator is Anti-Pattern

`TicketsController` asks for its dependency in the constructor:

```csharp
public TicketsController(ITicketRepository tickets)
{
    _tickets = tickets;
}
```

- The controller depends on the interface and never writes `new` or names the concrete class.
- The implementation can be swapped in `Program.cs` without touching the controller.
- It is easy to test with a fake repository.

Microsoft recommends requesting dependencies as constructor parameters and avoiding the service locator 
pattern, for example calling `GetService` to fetch a service yourself.


[Dependency injection in ASP.NET Core (Microsoft)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)

---

### Set up Options Pattern With Validation

---

### Why Secrets Don't Belong in appsettings.json

---

### Responsibility of Each Layer / Dependency Diagram

---

### Compare Technical Layering and Feature-Based Structure

---

## Sources
