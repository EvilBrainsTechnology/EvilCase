# Data and the domain model

`docs/product/vision.md` names the concepts and is what the model is built towards. This file holds only what the code does differently and what a new entity has to repeat. `Tests/EvilCase.Tests/Data/ApplicationDbModelTests` pins several of the rules below against the built model.

## Domain model

- **`Case`, and `@case` where it collides** — the domain's word is the code's word, so the type is `Case`, not a synonym invented to dodge a keyword. CA1716 flags identifiers matching a reserved word and is therefore set to `suggestion` in `src/.editorconfig`, the one rule deliberately below error. Where `case` would be a variable or lambda parameter, prefix it: `.Property(@case => @case.Status)`.
- **Every aggregate root carries `OwnerId` from its first migration**, with a foreign key to `Users` and an index — never added later as a data migration. Nothing filters on it until M8; until then a single user owns everything.
- **Nesting is a self-reference.** `ParentCaseId` is null on a root case and a sub-case has the same shape to any depth. Deleting a case cascades to its sub-tree; nothing deletes one yet.
- **`CaseTree`** (in `EvilCase.Data/Cases`) walks a loaded graph — descendants, ancestors, depth — and `CanNestUnder` is the only thing standing between the tree and a cycle. Every walk carries a visited set, so a graph that got a cycle anyway stops rather than hangs. It is pure over the navigation properties and says nothing about how the graph was loaded; a merged timeline over a whole sub-tree (M4) fetches it in one query instead of walking navigations.
- **Enums are stored as names**, `HasConversion<string>()` with an explicit length, as `UserRole` already is: an operator reads the column, and renumbering must not silently rewrite every row.
- **Tags are rows, not an array column** — free text, stored as typed, unique per case. The set of tags already in use is then an indexed query rather than a scan.
- **A file mark belongs to the proceeding, a file number to the document.** `CaseReference` holds the *spisová značka* of a case; the *číslo jednací* of one document belongs to the act it arrived with. Every authority in the chain assigns its own mark, so a case carries several at once and none of them is its identity.
- **The case's own mark is a column; everyone else's is a row.** `Case.InternalCaseReference` is required, generated on creation and unique per owner — a case always has exactly one, so it is not a row that could be missing or duplicated. `CaseReference` holds only marks assigned by somebody else, and `AssignedByPartyId` is therefore required.
- **An act's ordinal orders it, it does not identify it.** Deliberately not unique within a case: a real case file has two unrelated submissions filed under one number (`test-data/case-01-speeding.md`). Nothing may key on `(CaseId, Ordinal)`.
- **A date that a period runs from is a `DateOnly`.** An act's drafted, sent, delivered and received are calendar dates mapped to `date`, never `timestamptz` — a statutory deadline (M5) is counted in days and the hour never enters that arithmetic. Timestamps like `Created` stay `DateTime`.
- **A party outlives what names it.** Every foreign key from a case, a mark or an act to `Parties` is `DeleteBehavior.Restrict`, because a party accumulates history across all cases; the owning case cascades instead.

## Ownership

- **`IOwnerContext` is the one place ownership is resolved.** `EvilCase.Data` declares it; `PrincipalOwnerContext` in `EvilCase.Api` implements it by reading the access token's `sub` claim, and is the only code in the application that reads that claim for this purpose. A query needing the owner takes `IOwnerContext`, never an `ownerId` parameter threaded down from a controller and never `HttpContext` of its own.
- **`OwnerId` throws, `OwnerIdOrDefault` does not.** Code that has no sensible behaviour without an owner takes the first: a query that would otherwise return another owner's rows, or silently none, is a bug either way. The second is for the callers where absence is normal — a health probe, the sign-in endpoint, a migration at startup.
- **This is where a tenant goes.** When the vision's multi-tenant horizon becomes real, an owner becomes a tenant and this interface is what changes, rather than every query in the application.

## Reading a list

A screen that lists something reads it through one composed query, `CaseListQuery` (in `EvilCase.Data/Cases`) being the first of them. The shape holds for the act list, the timeline and the what-is-due view that follow.

- **One `IQueryable` step per rule**, each an extension on `IQueryable<TEntity>`: the roots, the search term, the status, the order, the projection. Composed by a reader (`ICaseReader`) that does nothing else, so what the list *is* reads top to bottom in one place, and each rule is pinned on its own by a test.
- **Nothing materialises early.** A step that returns a list rather than a queryable moves the filtering into the application and the paging out of reach. `ToListAsync` is called once, by the reader, at the end.
- **The projection is the column list.** Selecting the entity and shaping afterwards reads every column of every row and one query per row for the collections; projecting straight into the contract DTO reads what a row shows and nothing else.
- **A search term is text, not a pattern.** `%` and `_` in what the user typed are escaped and the escape character is named in the `ILIKE`, or a case titled *sleva 50%* is found by typing `%` — and so is every other case. Case folding belongs to `ILIKE`, never to a `ToLower()` that no index can use.
- **Tested without a server.** The design-time context factory names no connection string and `ToQueryString()` opens none, so what the SQL contains is a unit test — see `Tests/EvilCase.Tests/Cases/CaseListQueryTests`.

## Migrations

`Program.cs` awaits `MigrateEvilCaseDatabaseAsync` between `builder.Build()` and the middleware pipeline. A database that does not exist is created; an unreachable server stops the start, and there is no retry. `EvilBrains:EvilCase:Database:MigrateOnStartup` turns it off (default `true`) — do that where the schema is rolled out separately, or where several instances start at once, because nothing serialises concurrent migrators. `Tests/EvilCase.Tests` sets it to `false`.

**`dotnet ef migrations add` builds without `TreatWarningsAsErrors`**, unlike `dotnet r build`. A warning-level diagnostic in code written just before the migration — a `CS1574` cref that does not resolve, say — passes there, and the assembly it produces is then up to date, so later incremental builds skip recompiling it and never report the error. Run `dotnet build --no-incremental` (or `dotnet r ci` after touching the file again) once after adding a migration; a green `dotnet r build` straight afterwards proves nothing about the project the migration was generated from.

Migrations live in `EvilCase.Data.Migrations`, which references `EvilCase.Data` and cannot be referenced back. `UseEvilCaseMigrations` (in `EvilCase.Data`) names the assembly as a string and sets the `_MigrationsHistory` table name; EF loads the assembly at runtime and `EvilCase.Host` carries it into its output through an otherwise unused project reference. Both the runtime registration and `ApplicationDbContextFactory` must call that one extension — a mismatched history table makes EF re-apply every migration.
