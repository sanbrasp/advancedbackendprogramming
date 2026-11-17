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
- [ ] I can implement manual mapping from entity to DTO in the service layer.
- [ ] I can wire DbContext, repository, service and controller together with dependency injection.
- [ ] I use async/await and AsNoTracking() in all read operations.
- [ ] I can write a unit test for a service method by mocking the repository interface.

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

**Packages.** `Pomelo.EntityFrameworkCore.MySql` 9.0.0 is the provider, and it needs EF Core 9.0.x. The project
still targets .NET 10. Pomelo brought `MySqlConnector` 2.4.0, which gave warning NU1902: a known vulnerability
where the password can leak to a man-in-the-middle with certain TLS settings. Local connections are not affected,
but I installed `MySqlConnector` 2.6.1, the patched version, to fix it. Pomelo only requires 2.4.0 or newer.

[DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)  
[One-to-many relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many)  
[Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)  
[Pomelo.EntityFrameworkCore.MySql on NuGet](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql)  
[GHSA-473q-m89c-ghf8: MySqlConnector password disclosure](https://github.com/advisories/GHSA-473q-m89c-ghf8)

[🏠 To top](#overview)

---

## Use async / await and AsNoTracking() in all read operations

---

## Write unit test for service method by mocking the repo interface

---

## Quick quiz
From the summary page. Answered in my own words when the stages are done.
1.  A UserEntity has a PasswordHash field. What do I risk if I return it directly from an API endpoint?
2.  What is the difference between the Scoped, Transient and Singleton lifetimes in DI? Which one is used for DbContext, and why?
3.  I add a new field to an entity and run dotnet ef database update, but nothing changes in the database. What is the most likely cause?
4.  What is the N+1 problem, and how does Include() solve it?