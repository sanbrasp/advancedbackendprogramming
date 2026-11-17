# Advanced Backend Programming

Projects, notes, code and resources for the Advanced Backend Programming course @ Gokstad Akademiet.

---

## Project Structure

The solution holds the projects from each week. Help Desk (weeks 45 and 46) and Shop (week 47) are split into
layers: Core, Infrastructure and Api.

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

Shop.Core/                          What the shop's data is
├── DTOs/                           ProductDto, CategoryDto, OrderItemDto
└── Entities/                       Product, Category, OrderItem

Shop.Infrastructure/                Database access with EF Core and MySQL
├── Data/                           AppDbContext
└── Migrations/                     InitialCreate, AddStockQuantity and the model snapshot

Shop.Api/                           HTTP entry point and wiring
└── Program.cs                      Registers AppDbContext with the MySQL provider
```

---

## Tech Stack

| Technology   | Description                                          |
| ------------ | ---------------------------------------------------- |
| C#           | Programming language used for backend development    |
| .NET         | Framework for building and running C# applications   |
| ASP.NET Core | Web framework for building web applications and APIs |
| EF Core      | ORM that maps C# classes to database tables          |
| MySQL        | Relational database, run in Docker                   |
| Docker       | Runs the MySQL server in a container                 |


## Requirements

- .NET 10 SDK
- Docker Desktop (week 47)
- `dotnet-ef` 9.0.23: `dotnet tool install --global dotnet-ef --version 9.0.23`

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
dotnet run --project HelpDesk.Api
dotnet test
```

**Week 47 Project**

The database runs in Docker. Create `.env` next to `compose.yaml` first, with `MYSQL_ROOT_PASSWORD`, `MYSQL_USER`
and `MYSQL_PASSWORD`. It is not in the repository.

```text
docker compose up -d
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;port=3306;database=shopdb;user=<MYSQL_USER>;password=<MYSQL_PASSWORD>" --project Shop.Api
dotnet ef database update --project Shop.Infrastructure --startup-project Shop.Api
```

```bash
dotnet build
dotnet run --project Shop.Api
```

---

## Document Links
- [Week 44 Notes](Docs/Week44_Introduction.md)
  - [WebApiBasics README](WebApiBasics/README.md)
- [Week 45 Notes](Docs/Week45_InfrastructureArchitecture.md)
  - [HelpDesk.API README](HelpDesk.Api/README.md)
- [Week 46 Notes](Docs/Week46_RobustApis.md)
  - [HelpDesk.Api README](HelpDesk.Api/README.md)
- [Week 47 Notes](Docs/Week47_EntityFramework_Architecture.md)
  - [Shop.Api README](Shop.Api/README.md)

---
