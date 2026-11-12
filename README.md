# Advanced Backend Programming

Projects, notes, code and resources for the Advanced Backend Programming course @ Gokstad Akademiet.

---

## Project Structure

The solution has four projects. Arrows in the diagram mean "references".

```text
HelpDesk.Core/                      What a ticket is and what the app does with it
├── Interfaces/
│   ├── ITicketRepository.cs        Contract for the ticket store
│   └── ITicketService.cs           Contract for the ticket service
├── Records/
│   └── TicketRecord.cs             Ticket data model (record)
└── Services/
    └── TicketService.cs            Implements ITicketService, calls the repository

HelpDesk.Infrastructure/            Technical details Core needs but does not care about
└── Data/
    └── InMemoryTicketRepository.cs In-memory store, implements ITicketRepository

HelpDesk.Api/                       HTTP entry point and wiring
├── Controllers/                    Tickets, Diagnostics (lifetime demo)
├── DTOs/                           CreateTicketRequestDto
├── ExceptionHandlers/              GlobalExceptionHandler
├── Filters/                        AuditActionFilter, ValidateIdFilter
├── Interfaces/                     IOperation* (lifetime demo)
├── Middleware/                     ApiKeyMiddleware, RequestTimingMiddleware (+ extension)
├── Models/                         Operation (lifetime demo)
├── Settings/                       SecuritySettings, TicketSettings
├── Validators/                     CreateTicketRequestDtoValidator
└── Program.cs                      Service registration, Serilog setup and middleware pipeline

HelpDesk.Tests/                     xUnit integration tests for the API
├── Models/                         ThrowingTicketRepository (stub)
├── HelpDeskApiFactory.cs           Starts the API in memory with a test API key
└── TicketsApiTests.cs              Three integration tests
```

---

## Tech Stack

| Technology   | Description                                          |
| ------------ | ---------------------------------------------------- |
| C#           | Programming language used for backend development    |
| .NET         | Framework for building and running C# applications   |
| ASP.NET Core | Web framework for building web applications and APIs |


## Requirements

.NET 10 SDK

## Build and run

**Week 44 Project**  

```bash
dotnet build
dotnet run --project WebApiBasics
```

**Week 45 and 46 Project**  

The API key is not in the repository. Set it in user secrets first (use any made-up value):

```text
dotnet user-secrets set "Security:ApiKey" "<your-demo-key>" --project HelpDesk.Api
```

```bash
dotnet build
dotnet run --project Helpdesk.Api
dotnet test
```

---

## Document Links
- [Week 44 Notes](Docs/Week44_Introduction.md)
  - [WebApiBasics README](WebApiBasics/README.md)
- [Week 45 Notes](Docs/Week45_InfrastructureArchitecture.md)
  - [HelpDesk.API README](HelpDesk.Api/README.md)
- [Week 46 Notes](Docs/Week46_RobustApis.md)
  - [Helpdesk.Api README](HelpDesk.Api/README.md)

---
