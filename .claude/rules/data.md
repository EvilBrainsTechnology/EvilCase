---
paths:
  - "src/Data/**"
---

# Data

`EvilCase.Data` holds the schema and the way to reach the database (SDD-018). Read the model
SDDs under `docs/sdd/` and the fixtures under `Tests/EvilCase.Tests/Data/Model/` before changing
an entity; below is what neither says.

- The domain's word wins over a language keyword: the type is `Case`, with `@case` where it
  collides.
- A calendar date is `DateOnly` mapped to `date`; a moment in time stays `DateTime`.
- An index only where a query the code issues needs it; a unique index stands as a constraint.
- A test reads check constraints from `IDesignTimeModel`; `context.Model` has dropped them.
- The application reads and writes through `IDbSession.Current`, always through the entity's
  typed `DbSet`; a `DbContext` never leaves its DI scope.
- A read of one row ends in `Single` or `SingleOrDefault`; `First` only where more rows may match.
- A change to an entity changes the sample-data seeder in the same commit.

## Migrations

- Run `dotnet build --no-incremental` once after adding a migration.
- Never re-add a migration over its committed snapshot entry. Remove it first
  (`dotnet r remove-migration`), add it again, hand-format the result, and verify with
  `dotnet r generate-sql-script`.
- Rewrite a scaffolded rename by hand as `RenameColumn`, `RenameTable` and `RenameIndex`; rename
  a foreign key through `Sql`.
- A committed migration carries no `/// <inheritdoc />`; the scaffolder's are removed.
- The runtime registration and `ApplicationDbContextFactory` both call `UseEvilCaseMigrations`.
