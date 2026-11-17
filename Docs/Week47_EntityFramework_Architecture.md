# Shop API, Infrastructure, Core

Week 47 notes

---

## Overvirew
- [Week 44 (separate document)](../Docs/Week44_Introduction.md)
- [Week 45 (separate document)](../Docs/Week45_InfrastructureArchitecture.md)
- [Week 46 (separate document)](../Docs/Week46_RobustApis.md)
- [Week 47](#week-47)

---

## Week 47

Learning goals for this week:  
- [ ] I can explain what an ORM is, and what EF Core does for me compared with raw SQL.
- [ ] I can define an entity with Data Annotations and configure a relationship with the Fluent API.
- [ ] I can create and run a Code-First migration, and explain what happens in Up().
- [ ] I can justify why I should not return entities directly from the API.
- [ ] I can implement manual mapping from entity to DTO in the service layer.
- [ ] I can wire DbContext, repository, service and controller together with dependency injection.
- [ ] I use async/await and AsNoTracking() in all read operations.
- [ ] I can write a unit test for a service method by mocking the repository interface.

---

## What an ORM is, what EF Core does compared with raw SQL

---

## Define an entity with Data Annotations and configure relationship with the Fluent API

---

## Create and run a Code-First migration, explain what happens in `Up()`

---

## Justify why I should not return entities directly from API

---

## Implement manual mapping from entity to DTO in service layer

---

## Wire DbContext, repo, service and controller together with dependency injection

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