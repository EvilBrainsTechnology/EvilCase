---
paths:
  - "src/Data/**"
---

# Data

`docs/product/vision.md` names the domain concepts; `ApplicationDbModelTests` pins most of these.

- The domain's word is the code's word: the type is `Case`, with `@case` where the keyword
  collides — CA1716 sits at `suggestion` for this, the one rule below error.
- Every aggregate root carries `OwnerId` from its first migration, with a foreign key and an
  index.
- Cases form no hierarchy. A relation is one bare `CaseRelation` row, unique per pair of one
  owner's cases, `CaseId < RelatedCaseId`; deleting a case takes its relations and nothing else.
- Enums are stored as names: `HasConversion<string>()` with an explicit length.
- Tags are rows — typed, unique per case — never an array column.
- `Case.CaseNumber` is required and unique per owner, `Act.ActNumber` required and unique
  within its case; every external mark is an `ExternalCaseNumber` row with a required assigning
  party.
- The patterns both are issued from are one `NumberingSettings` row for the whole application,
  inserted by its migration and never seeded with `HasData` — a row the model holds is
  scaffolded as an `UpdateData` over whatever the operator saved. A series counts in a
  `NumberSequences` row unique on `(OwnerId, Scope)`.
- An act carries one required `Date` and no ordinal; acts are read ordered by it alone, and
  `(CaseId, Date)` is the index that serves both.
- A date a period runs from is `DateOnly` mapped to `date`; timestamps stay `DateTime`.
- Foreign keys to `Parties` are `DeleteBehavior.Restrict`; the owning case cascades instead.
- A file asset hangs on its primary act and carries the original name; another act reaches it
  through an `ActFileReference` carrying its own. No role anywhere. Deleting an act takes the
  assets filed under it and the references it made, and an asset another act still references
  refuses it. `FileAssets` is unique on `(OwnerId, ContentHash)`, never on the hash alone.
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
