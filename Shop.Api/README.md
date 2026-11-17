# Shop API

Week 47 - 2026

A practice project focused on Entity Framework Core and application architecture.

---

## Overview
- [About](#about)
- [Project Structure](#project-structure)
- [Shop API](#shop-api-1)
    - [Endpoints](#endpoints)
    - [Data Flow](#data-flow)
    - [Layers](#layers)
- [Migrations](#migrations)
- [Tests](#tests)
- [Sources](#sources)

---

## About

A small shop API for products, categories and order items, built in stages. The data flows from a MySQL table,
through an EF Core entity and DbContext, a repository and a service, and out of the API as a DTO.

The connection string is not in the repository. It is kept in user secrets.

---

## Project Structure

```text
Shop.Core/                              What the shop's data is
├── DTOs/
│   ├── CategoryDto.cs                  What the API returns for a category
│   ├── OrderItemDto.cs                 What the API returns for an order item
│   └── ProductDto.cs                   What the API returns for a product
└── Entities/
    ├── Category.cs                     Category entity, with a list of products
    ├── OrderItem.cs                    Order item entity
    └── Product.cs                      Product entity, linked to one category

Shop.Infrastructure/                    Database access
└── Data/
    └── AppDbContext.cs                 EF Core context: three DbSets and the Category-Product relationship

Shop.Api/                               HTTP entry point and wiring
└── Program.cs                          Registers AppDbContext with the MySQL provider
```

---

## Shop API

To be filled.

### Endpoints

To be filled.

### Data Flow

To be filled.

### Layers

To be filled.

---

## Migrations

To be filled.

---

## Tests

To be filled.

---

## Sources

To be filled.