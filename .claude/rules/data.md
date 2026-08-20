---
paths:
  - "src/Data/**"
---

# Data

`EvilCase.Data` holds the schema and the way to reach the database (SDD-018). Read the model
SDDs under `docs/sdd/` and the fixtures under `Tests/Data/Model/` before changing an entity;
below is what neither says.

- The domain's word wins over a language keyword: the type is `Case`, with `@case` where it
  collides.
- A calendar date is `DateOnly` mapped to `date`; a moment in time stays `DateTime`.
- A test reads check constraints from `IDesignTimeModel`; `context.Model` has dropped them.
- The application reads and writes through `IDbContextAccessor.Current`; a `DbContext` never
  leaves its DI scope.

## Migrations

- Run `dotnet build --no-incremental` once after adding a migration.
- Never re-add a migration over its committed snapshot entry. Remove it first
  (`dotnet r remove-migration`), add it again, hand-format the result, and verify with
  `dotnet r generate-sql-script`.
- Rewrite a scaffolded rename by hand as `RenameColumn`, `RenameTable` and `RenameIndex`; rename
  a foreign key through `Sql`.
- The runtime registration and `ApplicationDbContextFactory` both call `UseEvilCaseMigrations`.
