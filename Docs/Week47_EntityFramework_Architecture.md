# Shop API, Infrastructure, Core

Week 47 notes

---

## Overview
- [Week 44 (separate document)](../Docs/Week44_Introduction.md)
- [Week 45 (separate document)](../Docs/Week45_InfrastructureArchitecture.md)
- [Week 46 (separate document)](../Docs/Week46_RobustApis.md)
- [Week 47](#week-47)
  - [What an ORM is, and EF Core vs raw SQL](#what-an-orm-is-what-ef-core-does-compared-with-raw-sql)
  - [Entities, Data Annotations and the Fluent API](#define-an-entity-with-data-annotations-and-configure-relationship-with-the-fluent-api)
  - [Code-First migrations](#create-and-run-a-code-first-migration-explain-what-happens-in-up)
  - [Why not return entities from the API](#justify-why-i-should-not-return-entities-directly-from-api)
  - [Mapping from entity to DTO](#implement-manual-mapping-from-entity-to-dto-in-service-layer)
  - [DbContext, repository, service and controller with DI](#wire-dbcontext-repo-service-and-controller-together-with-dependency-injection)
  - [async/await and AsNoTracking()](#use-async--await-and-asnotracking-in-all-read-operations)
  - [Unit tests with a mocked repository](#write-unit-test-for-service-method-by-mocking-the-repo-interface)
  - [Quick quiz](#quick-quiz)

---

## Week 47

Learning goals for this week:  
- [x] I can explain what an ORM is, and what EF Core does for me compared with raw SQL.
- [x] I can define an entity with Data Annotations and configure a relationship with the Fluent API.
- [x] I can create and run a Code-First migration, and explain what happens in Up().
- [x] I can justify why I should not return entities directly from the API.
- [x] I can implement manual mapping from entity to DTO in the service layer.
- [x] I can wire DbContext, repository, service and controller together with dependency injection.
- [x] I use async/await and AsNoTracking() in all read operations.
- [x] I can write a unit test for a service method by mocking the repository interface.

---

## What an ORM is, what EF Core does compared with raw SQL

An ORM (Object-Relational Mapper) translates between C# objects and database tables. I describe the data as C#
classes, and the ORM writes the SQL. EF Core is Microsoft's ORM for .NET.

**Without an ORM (raw SQL with ADO.NET),** fetching all products means:

1. Open a connection.
2. Write the SQL as a string.
3. Loop over a `DataReader`.
4. Copy each column into a new object, and add it to a list by hand.

The SQL string is not checked when the code compiles, so a typo in a table or column name only shows up when it
runs. The code is also tied tightly to the database structure.

**With EF Core,** the same thing is one line:

```csharp
var products = await _context.Products.ToListAsync();
```

| | Raw SQL (ADO.NET) | EF Core |
|---|---|---|
| Queries | SQL strings, checked when the code runs | LINQ, checked by the compiler |
| Results | Copied into objects by hand | Mapped to entities automatically |
| Schema | Tables written and changed by hand | Created and changed with migrations |
| Control | Full control over every query | EF Core decides the SQL |

**What EF Core does for me:**

- Translates LINQ to SQL. The query runs against the database, and the provider (Pomelo for MySQL) writes SQL for
  that database.
- Maps the result rows to entities.
- Tracks changes to entities, so `SaveChangesAsync()` knows what to write back.
- Creates and updates the schema with migrations. In this project it wrote the `CREATE TABLE` statements for me.

**Code-First vs Database-First.** Code-First means I write the classes first and EF Core creates the tables. That
is what this course uses. Database-First means EF Core generates the classes from a database that already
exists.

**EF Core is not always the right tool.** For very complex queries, bulk operations or performance-critical code,
raw SQL through `FromSqlRaw`, or a lighter tool, can be a better fit.

[Overview of Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)  
[Querying Data](https://learn.microsoft.com/en-us/ef/core/querying/)

[🏠 To top](#overview)

---

## Define an entity with Data Annotations and configure relationship with the Fluent API

The three entities, `Product`, `Category` and `OrderItem`, live in `Shop.Core/Entities`. An entity is a class EF
Core maps to a table, and one object is one row.

- All properties are `public` with `{ get; set; }`. EF Core only maps public properties with a getter and a
  setter, so a field without `{ get; set; }` is ignored.
- `[Required]` and `[MaxLength]` set the name rules: 200 characters for `Product`, 100 for `Category`.
- An `int` key named `Id` gets its value from the database on insert, by convention.
- **Navigation property:** a property whose type is another entity. `Product.Category` is the category object the
  product belongs to, and `Category.Products` is the list of products in that category. `CategoryId` is the foreign
  key in the database, and the navigation properties are the C# side of the same link.
- `Products` is initialised to `new List<Product>()`, so it is never null. `Category` is `= null!;`, because EF
  Core fills it in.

**Public setters and security.** Access modifiers are a design tool, not a security feature. Reflection,
serialization and debugging can reach private members too. The real protection is that clients only ever see
DTOs, plus authentication, validation and secrets kept out of the code.

The relationship is configured with the Fluent API in `OnModelCreating` in `AppDbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasOne(p => p.Category)        // a product has one category
        .WithMany(c => c.Products)      // a category has many products
        .HasForeignKey(p => p.CategoryId);
}
```

- One `HasOne`/`WithMany` pair describes one relationship, from the `Product` side. Starting from `Category`
  with `HasMany`/`WithOne` describes the same thing from the other side.
- The lambda parameter is the object the lambda receives, not the property it returns. In `HasOne`, the lambda
  receives a `Product`, so it is `p`. In `WithMany`, it receives a `Category`, so it is `c`.
- `OnModelCreating` is `protected override`, because it overrides a `protected` method in `DbContext`, and an
  override must keep the same access level.
- Data Annotations are fine for simple rules like `[Required]`. The Fluent API is preferred for relationships,
  composite keys and indexes.

[Entity Properties - EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties)  
[Generated Values - EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties)  
[One-to-many relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many)  
[Securing State Data - .NET](https://learn.microsoft.com/en-us/dotnet/standard/security/securing-state-data)

[🏠 To top](#overview)

---

## Create and run a Code-First migration, explain what happens in `Up()`

**Database.** I had no local MySQL server, and `ServerVersion.AutoDetect` connects to the database even during
`migrations add`. So MySQL 8.4 runs in Docker, from `compose.yaml` in the solution folder. The passwords are in
`.env` next to it, and `.env` is in `.gitignore`.

- `MYSQL_DATABASE` creates `shopdb`, and `MYSQL_USER`/`MYSQL_PASSWORD` create a user with access to `shopdb`
  only. The app uses that user, not root.
- These variables are only read the first time the container starts with an empty volume. After a typo, I had to
  remove the volume too: `docker compose down -v --remove-orphans`, then `docker compose up -d`.
- The user secret must match `.env`. My first attempt failed with "Access denied", because the secret still held
  the placeholder user.

**Commands.** With separate projects, every command needs `--project` (where the migrations go: Infrastructure,
next to `AppDbContext`) and `--startup-project` (the Api, which has `Program.cs` and the user secret):

```powershell
dotnet ef migrations add InitialCreate --project Shop.Infrastructure --startup-project Shop.Api
dotnet ef migrations script --project Shop.Infrastructure --startup-project Shop.Api
dotnet ef migrations add AddStockQuantity --project Shop.Infrastructure --startup-project Shop.Api
dotnet ef database update --project Shop.Infrastructure --startup-project Shop.Api
```

`dotnet-ef` and `Microsoft.EntityFrameworkCore.Design` (in Shop.Api) are both version 9.0.23, to match EF Core 9.

**InitialCreate.**

- `Up()` creates the tables with their columns and rules, such as `NOT NULL` and max lengths. It sets the
  primary keys, adds the foreign key from `Products.CategoryId` to `Categories`, and creates an index on
  `CategoryId`.
- `Down()` undoes `Up()`: it drops the tables. `Products` goes before `Categories`, because of the foreign key.
- `migrations script` shows the SQL that `Up()` turns into: `CREATE TABLE`, `CREATE INDEX` and so on.

**AddStockQuantity.** The new migration only holds the change. EF Core compares the entities with
`AppDbContextModelSnapshot.cs` and finds the difference.

- `Up()` adds `StockQuantity` to `Products` as `int NOT NULL DEFAULT 0`. The default exists so rows already in
  the table get a value.
- `Down()` drops the column again.

**Running them.** `database update` ran both migrations and recorded each one in `__EFMigrationsHistory`. That
table is how EF Core knows which migrations are new. `Down()` runs when the database is rolled back, for
example with `dotnet ef database update InitialCreate`, or `dotnet ef database update 0` to undo everything.

Things I noticed in the SQL:

- The foreign key has `ON DELETE CASCADE`. `CategoryId` is a non-nullable `int`, so the relationship is
  required, and deleting a category also deletes its products.
- `decimal` became `decimal(65,30)`, because no precision was set.

Checked inside the container:

```powershell
docker exec -it shop-mysql mysql -u user -p shopdb -e "SHOW TABLES; DESCRIBE Products;"
```

[Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)  
[Managing Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing)  
[EF Core tools reference (.NET CLI)](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)  
[mysql - Official Image on Docker Hub](https://hub.docker.com/_/mysql)  
[docker compose down](https://docs.docker.com/reference/cli/docker/compose/down.md)

[🏠 To top](#overview)

---

## Justify why I should not return entities directly from API

The DTOs live in `Shop.Core/DTOs`, so the service can map to them. A DTO is a plain data class for the API, and
it holds only what the client needs.

```csharp
public record ProductDto(int Id, string Name, decimal Price, int CategoryId);
public record CategoryDto(int Id, string Name);
public record OrderItemDto(int Id, int ProductId, int Quantity, decimal UnitPrice);
```

- The navigation properties are left out. `Product` and `Category` point at each other, so returning them would
  create a circular reference when serialised to JSON.
- `CategoryId` stays in `ProductDto`. It is a plain `int`, so it cannot loop, and the client can see which category
  a product belongs to.
- The DTO keeps the API contract separate from the database structure, so a change to the entities does not break
  the clients.
- A DTO hides fields the client should not see, such as a password hash, and prevents over-posting.

[Tutorial: Create a controller-based web API with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0)
(section "Prevent over-posting")

[🏠 To top](#overview)

---

## Implement manual mapping from entity to DTO in service layer

The data flows one way: the repository returns **entities**, and the service maps them to **DTOs**. The
controller only ever sees DTOs.

**Repository.** `IProductRepository` lives in `Shop.Core/Interfaces`, and `ProductRepository` lives in
`Shop.Infrastructure/Repositories`. Both are `public`, because other projects implement or register them.

```csharp
public async Task<IEnumerable<Product>> GetAllAsync()
{
    return await _dbContext.Products.AsNoTracking().ToListAsync();
}

public async Task<Product?> GetByIdAsync(int id)
{
    return await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
}
```

- `AppDbContext` comes in through the constructor and is kept in a `private readonly` field. Nothing outside the
  repository should reach the context.
- `Product?` means "a product, or `null` if no product has that id".
- The theory page uses `FindAsync` for get-by-id, but `FindAsync` cannot be combined with `AsNoTracking()`.
  `FirstOrDefaultAsync` can.

**Service.** `IProductService` lives in `Shop.Core/Interfaces`, and `ProductService` lives in
`Shop.Core/Services`. The service depends on the repository **interface**, not the class, so it can be mocked in
tests.

```csharp
public async Task<IEnumerable<ProductDto>> GetAllAsync()
{
    var products = await _productRepository.GetAllAsync();

    return products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.CategoryId));
}

public async Task<ProductDto?> GetProductAsync(int id)
    {
      var result = await _productRepository.GetByIdAsync(id);
        
      return result == null ? null : new ProductDto(result.Id, result.Name, result.Price, result.CategoryId);
    }
```

- The mapping is manual: one `new ProductDto(...)` per entity. `Select` does it for every item in a list.
- My first version created the DTOs in a `foreach` loop but never kept or returned them.

[Designing the infrastructure persistence layer](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

[🏠 To top](#overview)

---

## Wire DbContext, repo, service and controller together with dependency injection

**DbContext (Stage 2).** `AppDbContext` lives in `Shop.Infrastructure/Data`:

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
}
```

- `public`, because `Program.cs` in `Shop.Api` registers it, and `internal` is only visible inside its own
  project. A top-level class cannot be `private`.
- The options say which database and provider to use, so they go to the base class with `: base(options)`.
- Each `DbSet<T>` is one table.

The connection string is in user secrets, never in the code:

```powershell
dotnet user-secrets init --project Shop.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;port=3306;database=shopdb;user=<user>;password=<password>" --project Shop.Api
```

Registration in `Program.cs`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
```

- `AddDbContext` registers the context as scoped: one instance per HTTP request.
- The `?? throw` stops the app at startup with a clear message if the connection string is missing.

**Repository and service (Stage 5).** Registered after `AddDbContext`:

```csharp
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
```

- Interface first, class second: "when someone asks for `IProductService`, make a `ProductService`".
- Scoped, because both depend on `AppDbContext`, which is scoped. A singleton must not depend on a scoped service.

**What the DI container is.** `builder.Services` is the list of registrations. `var app = builder.Build();`
turns that list into the container, an `IServiceProvider` object that lives in `app.Services` while the app runs.
There is no file or class for it in my code.

When `GET /api/products` comes in:

1. ASP.NET Core asks the container for a `ProductsController`.
2. Its constructor wants an `IProductService`, so the container makes a `ProductService`.
3. That one wants an `IProductRepository`, so it makes a `ProductRepository`.
4. That one wants an `AppDbContext`, so it makes one with the MySQL options.
5. It passes each object into the next constructor. I never write `new ProductService(...)` myself.

Scoped instances are kept for one request and thrown away when it ends. Singletons are kept for as long as the
app runs. Transient ones are never kept: a new one is made every time.

**Controller.** `ProductsController` lives in `Shop.Api/Controllers`. It is `public`, because ASP.NET Core only
finds public controllers, and it only knows `IProductService`, never the repository or the context.

```csharp
[HttpGet]
public async Task<IActionResult> GetAll()
{
    var products = await _productService.GetAllAsync();

    return Ok(products);
}

[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var product = await _productService.GetProductAsync(id);
    if (product is null)
        return NotFound();

    return Ok(product);
}
```

- `return product is null ? NotFound() : Ok(product);` works too. Since C# 9, the conditional takes its type from
  the return type, `IActionResult`, even though the two branches return different result types.
- My first version returned `Ok(_productService.GetAllAsync())` without `await`. That sends the unfinished
  `Task` to the client, not the products.

**Result.**

- `GET /api/products` returned `200` with `[]`, since the tables are empty. That proves the whole chain works:
  controller, service, repository, EF Core, MySQL.
- `GET /api/products/1` returned `404` as Problem Details with a `traceId`. `[ApiController]` turns a bare
  `NotFound()` into that format.
- The first call gave `500`: "Unable to connect to any of the specified MySQL hosts". The Docker container was
  stopped after a restart. `docker compose up -d` fixed it.

**Discussion: DbContext as a singleton instead of scoped.** One context would be shared by every request for as
long as the app runs.

- `DbContext` is not thread-safe, so two requests using it at the same time make EF Core throw an exception.
- The change tracker would keep every entity it has loaded, so memory keeps growing.
- Tracked entities can be returned from memory instead of being re-read, so the data can be stale.

Scoped avoids all three: each request gets its own fresh context, which is disposed when the request ends.

**Packages.** `Pomelo.EntityFrameworkCore.MySql` 9.0.0 is the provider, and it needs EF Core 9.0.x. The project
still targets .NET 10. Pomelo brought `MySqlConnector` 2.4.0, which gave warning NU1902: a known vulnerability
where the password can leak to a man-in-the-middle with certain TLS settings. Local connections are not affected,
but I installed `MySqlConnector` 2.6.1, the patched version, to fix it. Pomelo only requires 2.4.0 or newer.

[DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)  
[One-to-many relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many)  
[Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)  
[Pomelo.EntityFrameworkCore.MySql on NuGet](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql)  
[GHSA-473q-m89c-ghf8: MySqlConnector password disclosure](https://github.com/advisories/GHSA-473q-m89c-ghf8)  
[?: operator - C# reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator)

[🏠 To top](#overview)


---

## Use async / await and AsNoTracking() in all read operations

A database call is I/O-bound: the server waits for the database. With `await`, the thread is freed to serve
other requests while it waits. With a blocking call, it sits idle.

- A method that uses `await` needs `async` in its signature, and returns `Task<T>`.
- EF Core's async methods do the database work: `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`. They
  need `using Microsoft.EntityFrameworkCore;`.
- **Never `.Result`.** My first service used `_productRepository.GetAllAsync().Result`, which blocks the thread
  until the database answers. That is exactly what `async` is there to avoid.
- `AsNoTracking()` on every read. EF Core skips change tracking for those entities, which saves time and memory.
  The repository only reads, so both methods use it.

[Asynchronous programming scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)  
[Efficient Querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)

[🏠 To top](#overview)

---

## Write unit test for service method by mocking the repo interface

`Shop.Tests` is an xUnit project that references `Shop.Core` only, and uses the package `Moq`. These are **unit
tests**: no `WebApplicationFactory`, no HTTP and no database. Each test creates `ProductService` itself and passes
in a fake repository.

```csharp
var mockRepo = new Mock<IProductRepository>();
mockRepo.Setup(r => r.GetAllAsync())
    .ReturnsAsync(new List<Product>
    {
        new Product { Id = 1, Name = "Pen", Price = 100, CategoryId = 1, StockQuantity = 50 },
        new Product { Id = 2, Name = "Apple", Price = 200, CategoryId = 2, StockQuantity = 150 },
        new Product { Id = 3, Name = "Apple Pen", Price = 300, CategoryId = 3, StockQuantity = 12 },
    });
var service = new ProductService(mockRepo.Object);
```

- `new Mock<IProductRepository>()` makes Moq build a class that implements the interface.
- `Setup(...).ReturnsAsync(...)` decides what a method returns. The mock only answers what has been set up.
- `mockRepo.Object` is the fake repository itself, passed in instead of a real one.
- This only works because `ProductService` depends on the **interface**. A service that created its own
  repository with `new` could not be tested without a database.

| Test | Setup | Checks |
|---|---|---|
| `GetAllAsync_ReturnsThreeDtos_WhenRepositoryReturnsThreeProducts` | `GetAllAsync()` returns three products | The result has three DTOs |
| `GetProductAsync_ReturnsCorrectDto_WhenProductExists` | `GetByIdAsync(1)` returns one product | The DTO has the same `Id`, `Name`, `Price` and `CategoryId` |
| `GetProductAsync_ReturnsNull_WhenRepositoryReturnsNull` | `GetByIdAsync(99)` returns `null` | The result is `null` |

- `(Product?)null` tells the compiler which type the `null` is. A plain `null` is ambiguous, because
  `ReturnsAsync` has several versions.
- A whole number like `Price = 100` converts to `decimal` by itself. A number with a decimal point needs the `m`
  suffix, like `99.5m`, or it is a `double`.
- Moq is called a mocking library, but here the fake only returns data, and the asserts check the service's
  result. In the strict terms from last week, that makes it a stub.

**Result:** all three tests pass (19 Nov).

[Moq Quickstart](https://github.com/moq/moq4/wiki/quickstart)  
[Best practices for writing unit tests - .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

[🏠 To top](#overview)

---

## Quick quiz

From the summary page, answered when the stages were done.

**1. A `UserEntity` has a `PasswordHash` field. What do I risk if I return it directly from an API endpoint?**

The hash is sent to every client that calls the endpoint. An attacker can then try to crack it offline, with no
limit on attempts. Returning the entity also exposes the database structure and ties the API to the schema. The
fix is a DTO that leaves `PasswordHash` out.

**2. What is the difference between the Scoped, Transient and Singleton lifetimes in DI? Which one is used for
`DbContext`, and why?**

| Lifetime | Instances |
|---|---|
| Transient | A new one every time it is asked for |
| Scoped | One per HTTP request |
| Singleton | One for as long as the app runs |

`DbContext` is scoped. It is not thread-safe, so it must not be shared between requests, and its change tracker
should start empty for each request. As a singleton, it would crash under load, keep growing in memory and
return stale data.

**3. I add a new field to an entity and run `dotnet ef database update`, but nothing changes in the database. What
is the most likely cause?**

I forgot to create a migration first. `database update` only runs migrations that already exist and have not been
applied. The new field needs `dotnet ef migrations add <Name>` first, the way `StockQuantity` needed
`AddStockQuantity`.

**4. What is the N+1 problem, and how does `Include()` solve it?**

One query loads a list of N items, and then one extra query runs for each item to load its related data. That is
N+1 queries instead of one. For example, 100 products followed by 100 separate queries for their categories.
`Include()` loads the related data together with the main query (eager loading), for example
`_context.Products.Include(p => p.Category)`. Without `Include()`, the navigation property stays `null`, since
Web API does not use lazy loading by default.

[Efficient Querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)  
[DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

[🏠 To top](#overview)