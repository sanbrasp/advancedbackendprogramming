# Shop API, Infrastructure, Core

Week 47 notes

---

## Overview
- [Week 44 (separate document)](../Docs/Week44_Introduction.md)
- [Week 45 (separate document)](../Docs/Week45_InfrastructureArchitecture.md)
- [Week 46 (separate document)](../Docs/Week46_RobustApis.md)
- [Week 47](#week-47)

---

## Week 47

Learning goals for this week:  
- [x] I can explain what an ORM is, and what EF Core does for me compared with raw SQL.
- [x] I can define an entity with Data Annotations and configure a relationship with the Fluent API.
- [ ] I can create and run a Code-First migration, and explain what happens in Up().
- [x] I can justify why I should not return entities directly from the API.
- [ ] I can implement manual mapping from entity to DTO in the service layer.
- [x] I can wire DbContext, repository, service and controller together with dependency injection.
- [ ] I use async/await and AsNoTracking() in all read operations.
- [ ] I can write a unit test for a service method by mocking the repository interface.

---

## What an ORM is, what EF Core does compared with raw SQL

An ORM translates C# objects into database tables.

Add more info here.

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