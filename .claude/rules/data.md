---
paths:
  - "src/Data/**"
---

# Data

`docs/product/vision.md` names the domain concepts; `ApplicationDbModelTests` pins most of the
rules below against the built model.

- The domain's word is the code's word: the type is `Case`, with `@case` where the keyword
  collides — CA1716 sits at `suggestion` for this, the one rule below error.
- Every aggregate root carries `OwnerId` from its first migration, with a foreign key and an
  index.
- A sub-case is a self-reference (`ParentCaseId`); deleting a case cascades to its sub-tree.
- Tree walks (`CaseTree`) stay pure over navigation properties and carry a visited set.
- Enums are stored as names: `HasConversion<string>()` with an explicit length.
- Tags are rows — typed, unique per case — never an array column.
- `Case.CaseNumber` is required and unique per owner; every external mark is an
  `ExternalCaseNumber` row with a required assigning party.
- An act carries one required `Date` and no ordinal; acts are read ordered by it alone, and
  `(CaseId, Date)` is the index that serves both.
- A date a period runs from is `DateOnly` mapped to `date`; timestamps stay `DateTime`.
- Foreign keys to `Parties` are `DeleteBehavior.Restrict`; the owning case cascades instead.
- `FileAssets` is unique on `(OwnerId, ContentHash)`, never on the hash alone.
- A comment hangs on a case XOR an act: one table, two nullable parents, a check constraint.
  Tests read check constraints from `IDesignTimeModel`, not `context.Model`.

## Migrations

- `dotnet ef migrations add` builds without `TreatWarningsAsErrors`: run
  `dotnet build --no-incremental` (or `dotnet r ci` after touching the file again) once after
  adding a migration; a green incremental build straight afterwards proves nothing.
- Never re-add a migration over its committed snapshot entry — it comes out empty. Remove it
  first (`dotnet r remove-migration`), add it again, hand-format the result, and verify with
  `dotnet r generate-sql-script`; `MigrationsTests` replays every `Up`.
- A rename is scaffolded as a drop and a create: rewrite it by hand as `RenameColumn`,
  `RenameTable` and `RenameIndex`, and rename a foreign key through `Sql` — nothing models it.
- The runtime registration and `ApplicationDbContextFactory` both call `UseEvilCaseMigrations` —
  a mismatched history table re-applies every migration.
