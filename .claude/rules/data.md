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
  collides — CA1716 sits at `suggestion` for this, the one rule below error.
- A new aggregate root carries `OwnerId` from its first migration, with a foreign key and an
  index; the tests check the entities that exist, not the one you are adding.
- A date a period runs from is `DateOnly` mapped to `date`; timestamps stay `DateTime`.
- Tags are rows — typed, unique per case — never an array column.
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
