---
paths:
  - "src/Data/**"
---

# Data

`EvilCase.Data` is schema only, and the model is written down twice already:
`docs/product/vision.md` names the domain concepts, `ApplicationDbModelTests` pins the schema —
one rule per test, the reason in its assertion. Read them before changing an entity. Below is
what neither says.

- The domain's word is the code's word: the type is `Case`, with `@case` where the keyword
  collides.
- A date a period runs from is `DateOnly` mapped to `date`; timestamps stay `DateTime`.
- A test reads check constraints from `IDesignTimeModel`; `context.Model` has dropped them.

## Migrations

- `dotnet ef migrations add` builds without `TreatWarningsAsErrors`. Run
  `dotnet build --no-incremental` once after adding a migration; a green incremental build
  straight afterwards proves nothing.
- Never re-add a migration over its committed snapshot entry — it comes out empty. Remove it
  first (`dotnet r remove-migration`), add it again, hand-format the result, and verify with
  `dotnet r generate-sql-script`; `MigrationsTests` replays every `Up`.
- A rename is scaffolded as a drop and a create: rewrite it by hand as `RenameColumn`,
  `RenameTable` and `RenameIndex`, and rename a foreign key through `Sql` — nothing models it.
- The runtime registration and `ApplicationDbContextFactory` both call `UseEvilCaseMigrations` —
  a mismatched history table re-applies every migration.
