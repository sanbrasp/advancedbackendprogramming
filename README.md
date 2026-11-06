# Advanced Backend Programming

Projects, notes, code and resources for the Advanced Backend Programming course @ Gokstad Akademiet.

---

## Solution Structure

```text
AdvancedProgrammingSolution/
├── HelpDesk.Api/           Week 45 goals
│   └── README.ms           Helpdesk API README
├── WebApiBasics/           Week 44 goals
│   └── README.md           Web API Basics README
├── Docs/
│   └── Week44_Introduction.md
│   └── Week45_InfrastructureArchitecture.md
└── README.md               Solution README
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

**Week 45 Project**  

The API key is not in the repository. Set it in user secrets first (use any made-up value):

```text
dotnet user-secrets set "Security:ApiKey" "<your-demo-key>"
```

```bash
dotnet build
dotnet run --project Helpdesk.Api
```

---

## Document Links
- [Week 44 Notes](Docs/Week44_Introduction.md)
  - [WebApiBasics README](WebApiBasics/README.md)
- [Week 45 Notes](Docs/Week45_InfrastructureArchitecture.md)
  - [HelpDesk.API README](HelpDesk.Api/README.md)

---
