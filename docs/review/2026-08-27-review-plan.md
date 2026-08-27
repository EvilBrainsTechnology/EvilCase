# Review plan — 2026-08-27

Whole-codebase review before further feature work: uniformity, rules and SDD compliance,
security, UI consistency, complexity, dependencies. Build is clean (0 warnings), CI on master
is green, `dotnet list package --vulnerable --include-transitive` reports nothing, `.env` was
never tracked. Local test failures in this session are the missing Docker daemon
(Testcontainers), not the code.

## Summary

The codebase is in very good shape: layering, tenancy, auth, numbering and the test suite all
hold their SDDs; no Critical or High security finding exists. The five biggest problems:

1. ~950 lines of dead library surface in `EvilBrains.Collections` and the entire unused
   `EvilBrains.EntityFramework` project (R-013, R-014).
2. Set-based writes (`ExecuteUpdate`/`ExecuteDelete`) skip the per-user ownership rule
   SDD-006 mandates — latent while 1 user = 1 tenant (R-001).
3. Contact detail never shows occurrences via external act numbers, so a referenced contact
   can show zero occurrences yet refuse deletion with 409 (R-006).
4. ~600 lines of frontend and test duplication with proven negative-cost folds
   (R-034, R-035, R-037, R-038).
5. SDDs drifted in details: error-status table, route table, toast rule, login layout
   (R-007 … R-012).

## Findings

### Security

**R-001 · Medium · M** — Set-based writes do not enforce per-user ownership.
Where: `CaseWriter.cs:111-121,153-155`, `ActWriter.cs:119-128,150-153`, `FileWriter.cs:55-58,82-85`,
`ExternalCaseNumberWriter.cs:54-57`, `ExternalActNumberWriter.cs:60-63`.
SDD-006 decides another user's row in the tenant cannot be written, changed or deleted;
SDD-018 makes `ExecuteUpdate`/`ExecuteDelete` responsible for what the interceptor otherwise
does. Only `CommentWriter` repeats the user in the write predicate. Unreachable today
(registration closed, 1 user = 1 tenant), becomes IDOR the day a tenant holds two users.
Change: per decision Q1 — mirror `CommentWriter` (read owner, answer 403) in the five writers,
or narrow SDD-006. Risk: none today; changes future 404/403 semantics.

**R-002 · Low · S** — JSON enum binding accepts undefined integer values.
Where: `CaseStatus.cs:9`, `ActDirection.cs:9`, `ContactKind.cs:11` (`JsonStringEnumConverter<T>`
defaults to `allowIntegerValues: true`); e.g. `CaseEditRequest.Status`.
`{"status": 999}` binds, persists as string `"999"` and rides in the Open filter — bypasses the
SDD-004 validation layer. Change: a small
`StrictJsonStringEnumConverter<T> : JsonStringEnumConverter<T>` with `allowIntegerValues: false`
in `Api.Contract`, applied to the three enums; test per enum. Risk: none; unknown names already 400.

**R-003 · Low · S** — Upload metadata is not validated server-side.
Where: `FileTransferController.cs:33-34` → `FileAsset.cs:28,42` (256/128 max lengths).
A 300-char filename or media type hits `DbUpdateException` → 500 after the blob was written
(attacker-triggerable orphan blobs); an empty filename passes. Change: validate length and
non-emptiness before `StoreFile`, answer 400; test. Risk: none.

**R-004 · Low · S** — No `Permissions-Policy` header; HSTS at framework defaults.
Where: `SecurityHeadersMiddleware.cs:27-37`, `Program.cs:169`.
Change: per decision Q5 — add a minimal deny `Permissions-Policy`
(`camera=(), microphone=(), geolocation=()`), extend `SecurityHeadersTests`; leave HSTS.
Security headers are an owner decision (`code.md`). Risk: none known; CSP already tight.

**R-005 · Low · S** — Rate-limit partitions treat every IPv6 address as distinct.
Where: `Program.cs:219-222`. One /64 yields unlimited login partitions; account lockout still
bounds per-account guessing. Change: partition IPv6 by /64 prefix in `ClientAddress`; test.
Risk: none.

### SDD compliance

**R-006 · code wrong (SDD-011) · M** — Contact occurrences miss the external-act-number source.
Where: `ContactReader.cs:33-53`, `ContactOccurrenceQuery.cs` (no query over
`ExternalActNumber.AssignedByContactId`); dead scaffolding `ContactActRole.cs:16` (`NumberIssuer`),
`ContactActRoleDisplay.cs:13`, `ContactActOccurrence.ExternalNumber` always null,
always-empty column `ContactActOccurrences.razor:37`.
SDD-011 lists occurrences "úkony přes … externí čísla"; a contact referenced only by an external
act number shows zero occurrences yet answers 409 on delete. Change: fourth query + concat in
the reader, fill `ExternalNumber`, `Role = NumberIssuer`; database test. Risk: ordering of merged
occurrence rows needs a deterministic key.

**R-007 · SDD outdated · S** — SDD-004's status table misses statuses the code answers.
403 non-author on comments (`CommentWriteAnswer.cs:16-17`), 423 lockout (`AuthController.cs:34`),
429 rate limit, 413 upload. Change: add the rows; keep "cizí tenant nikdy 403" — it holds.

**R-008 · SDD outdated · S** — SDD-005 misses implemented routes and the client-generator exception.
External-number endpoints (`CasesController.cs:98,127`, `ActsController.cs:99,129`),
`GET /api/contacts/default` (`ContactsController.cs:33`), and the hand-written
`FileTransferClient` for multipart/stream (generator cannot express it). Change: extend the route
table; one sentence naming the file-transfer exception.

**R-009 · needs owner (Q2) · S** — 404 vs 409 for a body-referenced id that does not exist.
Unknown contact on act create/edit → 404 (`ActsController.cs:45,76`); unknown contact on
external-number add → 409 (`CasesController.cs:115-118`); unknown parent → 409. Change: per Q2,
unify (recommended: body-referenced missing id = 409, route id = 404; act contact moves to 409)
and state the rule in SDD-004. Risk: wire behavior change on act create/edit; frontend messages
follow.

**R-010 · needs owner (Q3) · M** — SDD-004's "selhání sítě ukáže toast" is implemented nowhere.
`ToastContainer` is mounted but never fed (`MainLayout.razor:4`; no `IToastService` usage);
every failure renders an inline `.empty`/alert consistently. Worst case: `ContactPicker.razor:96-102`
swallows a network failure into "Žádný kontakt neodpovídá. Založte nový." Change: per Q3 —
recommended: SDD-004 adopts the inline pattern, `ToastContainer` goes, `ContactPicker` gets a real
failure state. Risk: none.

**R-011 · SDD imprecision · S** — SDD-016 says every page lives in `MainLayout`; `Login.razor:2`
necessarily uses `LoginLayout`. Change: SDD-016 names the login exception.

**R-012 · SDD gap · S** — Bulk drop refuses a batch over 100 files wholesale
(`FilesCard.razor:119,188-193`); SDD-012 says only the failing file is refused. Change: SDD-012
states the batch cap (recommended: keep the cap, document it).

### Simplification

**R-013 · needs owner (Q4) · L** — ~95 % of `EvilBrains.Collections` is unused.
Application usage is exactly `TrimEmptyToNull` (4 sites), string `NullIfEmpty` (2 sites),
`AsReadOnlyList` (1 site). Dead: `Factories/*` (397 lines), `WhenAllExtensions`,
`StringJoinExtensions`, `AsReadOnlyAsyncExtensions`, `AsAsyncEnumerableExtensions`,
`EmptyIfNullExtensions`, collection `NullIfEmpty` overloads — BCL reinvention
(`Task.WhenAll`, `string.Join`, collection expressions). Change: trim the library to the three
used members; drop the four dead test files. ~880 lines removed; what remains is a ~35-line
library. Risk: none inside this repo (usage grep-verified incl. Tests and .razor).

**R-014 · needs owner (Q4) · S** — `EvilBrains.EntityFramework` is a dead project.
Zero usings anywhere; referenced only by `EvilCase.Data.csproj:12` and `EvilCase.slnx:33`.
Change: delete the project and both references. ~47 lines. Risk: none.

**R-015 · S** — Dead dependency lines.
`EvilCase.Api.csproj:22-23` reference `EvilBrains.Collections` and `EvilBrains.Cryptography`
without a single using; `Directory.Packages.props:17` pins `Microsoft.Extensions.Configuration`
no project references; `EvilBrains.Logging.AspNetCore.csproj:9` references
`Serilog.Extensions.Hosting` whose API it never uses. Change: remove; build verifies the Serilog
one. Risk: none expected.

**R-016 · S** — Dead Serilog enricher configuration.
`appsettings.json:67` names `WithMachineName`/`WithThreadId`; neither enricher package is
referenced, so the entries bind to nothing; `ReservedLogPropertyNames.cs:28-29` reserves the two
names as if attached. Change: drop the two entries and the two reserved names (or add the
packages — not recommended, single-container deployment). Risk: none.

**R-017 · S** — Small Host/bootstrap folds.
Config-path parameters with one constant caller (`AddEvilCaseAuth`/`AddEvilCaseFiles`,
`Program.cs:119-120`); private single-use generic `AddLocalDbContext<TContext>`
(`Data/Bootstrap.cs:42-65`); leftover `"AllowedHosts": "*"` in `appsettings.Development.json:22`.
Change: fold the constants in, inline the helper, drop the line. ~12 lines. Risk: none.

### Backend uniformity

**R-018 · S/M** — `CommentWriter` case and act triplets are near-identical (~58 duplicated lines,
`CommentWriter.cs:14-71` vs `:73-130`). Change: private cores (add/edit/remove) taking the owner
probe and the scoped `IQueryable`; public methods become wrappers. ~−40 lines.

**R-019 · S** — `FileWriter` delete pair identical modulo one query step (`FileWriter.cs:41-66`
vs `:68-93`). Change: private `DeleteFile(IQueryable<FileAsset>, …)`. ~−20 lines.

**R-020 · S** — `FileTransferController` upload preamble duplicated verbatim
(`FileTransferController.cs:25-36` vs `:56-67`). Change: private `BuildUpload(IFormFile)`.
~−12 lines.

**R-021 · S** — Owner-existence probe spelled out 11×
(`Cases.WithId(caseId).AnyAsync` 5×, `Acts.OfCase(caseId).WithId(actId).AnyAsync` 6×).
Change: two query steps beside `EntityQuery`. ~−10 lines, one spelling of the ownership rule.

**R-022 · S** — `ContactOccurrenceQuery.cs:48-61` vs `:63-76` differ only in the `Role` constant.
Change: one `AsActOccurrences(role)`. ~−12 lines. Do after R-006 (same file).

**R-023 · S** — `ExternalActNumberQuery.OfAct(actId)` scopes by act alone while files and comments
scope by `(caseId, actId)`; forces the extra probe in `ExternalActNumberWriter.cs:56-58`.
Change: `OfAct(caseId, actId)` via `Act!.CaseId`; drop the probe. ~−5 lines.

**R-024 · S** — Problem titles inlined as literals where constants exist: "Case not found" 7×,
"Contact not found" 3×, "File not found" 3× (e.g. `CasesController.cs:55,71,93,114`,
`ContactsController.cs:47,62,78`); the App matches on these strings. Change: constants in
`CaseProblems`/`ContactProblems`/`FileProblems`; `ActProblems` keeps only its own.

**R-025 · S** — Update outcome precedence differs: `ActWriter.cs:91-97` checks existence first
(with the rule stated in a comment); `CaseWriter.cs:85-99` validates number first, so a missing
case with a malformed number answers 400. Change: exists-first in `UpdateCase`; behavior test.
Risk: wire behavior change for one edge case.

**R-026 · S** — Create stores untrimmed text, update trims (`CaseWriter.BuildCase`,
`ActWriter.BuildAct` vs both Update paths). Change: trim in both builds; drop the one-caller
`CaseWriter.Normalize` (code.md single-call-site rule) by giving both writers the same inline
shape; test. Risk: none.

**R-027 · S** — `ContactWriter.cs:60` uses an inline predicate and `entity` naming instead of
`.WithId`; `:25-27` reads `dbSession.Current` twice. Change: `.WithId(contactId)`, a `context`
local. ±0 lines.

**R-028 · S** — Six query-step classes are `public` (`ActListQuery`, `CaseListQuery`,
`ContactListQuery`, `CaseNumberQuery`, `ActNumberQuery`, `EntityQuery`) though only
`InternalsVisibleTo` tests consume them; eight siblings are `internal`. Change: all internal.

**R-029 · S** — All 11 entity records are unsealed while every contract record is sealed; nothing
derives from any entity. Change: seal them.

**R-030 · S** — Dead switch arms: explicit `NotAuthor => throw new UnreachableException()`
duplicating the discard arm (`CaseCommentsController.cs:34`, `ActCommentsController.cs:36`).
Change: delete. −2 lines.

**R-031 · needs owner (Q7) · S** — Five byte-identical `{Deleted, NotFound}` outcome enums
(`CaseDeleteOutcome`, `ActDeleteOutcome`, `FileDeleteOutcome`, both external-number deletes).
Change: per Q7, one shared `DeleteOutcome`; −4 files, ~−32 lines. Counterweight: per-entity enums
can grow apart (`ContactDeleteOutcome` already did).

**R-032 · S** — Writer logging is uneven: `ContactWriter` logs nothing, `CommentWriter` skips
update, external-number writers skip delete, the rest log every mutation. Change: log every
business mutation (matches SDD-002's identifiers-only style). ~+6 lines.

**R-033 · S** — Owner-missing outcome naming differs: `CaseNotFound`/`ActNotFound` vs
`UploadFileOutcome.OwnerNotFound` vs `CommentWriteOutcome.NotFound`. Change: owner-named members
everywhere; renames only, with R-031.

### Tests

**R-034 · S** — Byte-identical 13-line `AssertProblem` in 8 controller fixtures
(`CasesControllerTests.cs:402` and 7 more). Change: one shared helper. ~−90 lines.

**R-035 · M** — The `TestTenant` SetUp/TearDown scaffold repeats in 26 fixtures
(`CommentWriterTests.cs:22-33` and `ActCommentWriterTests.cs:22-33` are identical to the letter);
`UserWriteInterceptorTests` repeats a 3-line arrange in all 10 tests. Change: a small base
fixture holding the tenant lifecycle; a field in the interceptor fixture. ~−280 lines.
Risk: touches 26 files; mechanical.

**R-036 · S** — Utils fixtures use a `…Test` method suffix the rest of the solution does not
(`DbSetAccessAnalyzerTests.cs:11-129` et al.); one helper named `Foo`
(`AwaitEnumerableTests.cs:29`). Change: rename; parts die with R-013's test files anyway.

### Frontend

**R-037 · M** — Six delete modals share a 39-line frame (434 lines total,
`ActDeleteModal.razor` … `FileDeleteModal.razor`). Change: shared `ConfirmDeleteModal`
(message child content, delete delegate, error strings, optional extra `ApiException` mapper);
six thin wrappers. ~−190 lines. Risk: modal behavior must stay identical — busy state, close on
success, NotFound/Forbidden/Conflict messages.

**R-038 · M** — `ContactCreateModal` and `ContactEditModal` are near-copies (213 lines, 48
markup lines identical). Change: one `ContactFormModal` with optional `ContactDetail?`.
~−90 lines.

**R-039 · S/M** — The notFound/failure/spinner triple repeats verbatim on 5 detail/edit pages
(95 lines). Change: `LoadStateView` component. ~−30 lines. Do after R-037 sets the pattern.

**R-040 · S** — `Contacts.razor:54-66` is the only `QuickTable` (with a fragile column-order
coupling comment); every other list is a plain Tabler `<table>`. Change: plain table on Contacts;
the asc/desc toggle on Name goes (nothing else uses one). Risk: loses that toggle.

**R-041 · S** — `app.css:68` says `.list-group-item-actions` (plural); the real class is
`list-group-item-action`, so `ContactPicker` results escape the 44 px touch-target rule.
Change: drop the `s`.

**R-042 · S** — `UserMenu.razor:9,19` branch at `xl`; app.md allows only `lg`. Change: `lg`.

**R-043 · S** — `PageTitle` missing on `Home`, `Cases`, `Contacts`, `NewCase`, `NewAct`.
Change: one line each.

**R-044 · S** — `NewAct.razor:132-137` maps no 404, so a deleted picked contact yields a generic
error; `EditAct.razor:227-232` distinguishes both 404 titles. Change: same catch in NewAct.

**R-045 · S** — Comment edit form (`CommentsCard.razor:57-60`) flips the Zrušit/Uložit order and
lacks the busy spinner every other form has. Change: reorder, add spinner.

**R-046 · S** — `NewAct.razor:54,60` picker labels lack `for`; `EditAct` has them. Change: add.

**R-047 · S** — Czech terminology drift: Odesílatel/Příjemce vs Od/Komu (`ActListCard.razor:81,84`)
vs Vydal/Adresát (`ContactActRoleDisplay.cs:11-12`); "ID datové schránky" vs "Datová schránka";
two search-placeholder phrasings; "Nový kontakt" modal submitting "Uložit" where creates say
"Založit …". Change: odesílatel/příjemce everywhere, "ID datové schránky", one placeholder
phrasing, "Založit kontakt".

**R-048 · S** — Empty-state style drift (sentence-with-period as title in
`ContactCaseOccurrences.razor:11`, `FilesCard.razor:60` vs noun labels elsewhere); row delete
buttons plain in `ExternalNumbersCard.razor:43,60` vs `btn-danger` elsewhere. Change: align both.

**R-049 · S** — `ContactCaseOccurrences.razor:31-32` links the case number, every other table
links the title. Change: link the title.

### Rules

**R-050 · S** — Two `code.md` lines need rewording, no net lines: the identifier rule never
exempts the entity key (all 11 entities rightly declare bare `Id`) — append "; an entity's key
property is `Id`"; the assertion-message rule does not say when a message is required (half the
suite has none) — append "where the assert alone does not say it".

**R-051 · S** — One unwritten convention worth one line (budget: 538/539 total lines, so exactly
one fits): `- A business write returns an outcome enum named <Entity><Verb>Outcome; the action
maps every member and throws UnreachableException for the rest.` Would have caught
`UploadFileOutcome`'s naming (R-033). Further candidate rules (sealed contract records, test
naming, fixture pattern, double naming) do not fit the limit and are declined.

## Decisions needed from the owner

1. **R-001, set-based per-user enforcement.** (a) Enforce now: the five writers mirror
   `CommentWriter` (read owner, answer 403 non-owner), SDD-004 gains the 403 row via R-007;
   (b) narrow SDD-006 to SaveChanges writes until multi-user tenants exist.
   Recommendation: (a) — cheap, closes a latent IDOR, keeps the SDD true.
2. **R-009, missing body-referenced id.** (a) Unify: body-referenced missing id = 409, route
   id = 404 (act create/edit `ContactNotFound` moves 404 → 409); (b) keep per-endpoint status
   quo and document it. Recommendation: (a) — one rule, matches external numbers and parent.
3. **R-010, network-failure UX.** (a) SDD-004 adopts the inline alert pattern; `ToastContainer`
   goes; `ContactPicker` gets a failure state; (b) implement toasts everywhere.
   Recommendation: (a) — the inline pattern is implemented, consistent and works.
4. **R-013/R-014, Utils trim.** Is any `EvilBrains.*` library consumed outside this repository?
   If not: trim `EvilBrains.Collections` to its three used members and delete
   `EvilBrains.EntityFramework`. Recommendation: trim — git keeps the history.
5. **R-004, security headers.** Add the minimal deny `Permissions-Policy`; leave HSTS at
   defaults. Owner-gated by `code.md`. Recommendation: add it.
6. **External numbers have no edit** (vision: everything the user enters can be edited); today it
   is delete-and-re-add (`ExternalNumbersCard`). (a) Accept and note in SDD-009/010; (b) build
   edit endpoints + UI. Recommendation: (a) — two-field rows, re-add costs seconds.
7. **R-031, delete-outcome enums.** Merge the five identical `{Deleted, NotFound}` enums into one
   shared `DeleteOutcome`, or keep per-entity enums for future divergence.
   Recommendation: merge — `ContactDeleteOutcome` shows a diverging entity gets its own anyway.

## Execution order

Each batch builds, tests green, one commit (`review: R-0xx, R-0yy — <what>`); SDD and rule edits
in separate commits at the end so they describe the finished state.

- **Batch 1 — security fixes:** R-002, R-003, R-005 (+ tests).
- **Batch 2 — security decisions applied (after Q1, Q5):** R-001, R-004 (+ tests).
- **Batch 3 — deletions (after Q4):** R-013, R-014, R-015, R-016, R-017.
- **Batch 4 — SDD gap:** R-006 (+ database test).
- **Batch 5 — backend folds:** R-018, R-019, R-020, R-021, R-022, R-023.
- **Batch 6 — backend consistency:** R-024, R-025, R-026, R-027, R-030 (+ behavior tests).
- **Batch 7 — backend surface (after Q7):** R-028, R-029, R-031, R-032, R-033.
- **Batch 8 — tests:** R-034, R-035, R-036.
- **Batch 9 — frontend structure (after Q3):** R-037, R-038, R-039, R-040, ContactPicker part
  of R-010.
- **Batch 10 — frontend polish:** R-041 … R-049.
- **Batch 11 — SDD updates (after Q1, Q2, Q3, Q6):** R-007, R-008, R-011, R-012 and the
  decision outcomes; separate commits, meta-edit flow.
- **Batch 12 — rule updates:** R-050, R-051; meta-edit flow.

Batches 1, 4, 5, 6, 8, 10 need no decision and can start on approval alone.

## Deliberately unchanged

- `ExternalCaseNumberWriter` vs `ExternalActNumberWriter` (~90 % identical, 38 lines each): two
  sites, below the repository's own 3+ threshold; a generic fold costs more than it saves. A
  third external-number owner tips the scale.
- Case/act controller pairs: forced by route templates and the client generator; the shared
  outcome switch is already extracted.
- `GET /api/auth/sessions` returning a bare list instead of a `*ListResponse`: a wire change with
  no user value inside the closed auth module.
- Custom `PasswordHasher`: OWASP-parameter PBKDF2, constant-time compare, timing decoy, tested;
  switching to Identity's is an owner-level auth change with no gain.
- The API client generator: an explicit SDD-001 decision; replacing it is a rewrite of working
  code.
- Hand-rolled test doubles (no mocking library): deliberate, consistently placed; a library is a
  dependency decision with style cost.
- `IUserContext.UserIdOrDefault`, `ClientInfo.Unknown`, the `Parse` halves of the numbering
  types: sanctioned symmetry (`Parse`/`ParseOrDefault` rule) or test-consumed surface.
- Cases/Contacts search `@code` similarity: a shared abstraction is break-even (~45 lines saved,
  ~40 added).
- Writer-test file slicing differences: churn without content win.
- `EvilBrains.Dispose` as a one-class project: folding it into a consumer crosses the
  Utils-independent-of-EvilCase boundary.
- Forwarded-headers trust model and the anonymous log-upload flood bound: documented deployment
  tradeoffs (`Program.cs:51-64,79`); see follow-ups.
- Seed administrator e-mail in the startup log: an operator identifier, within SDD-002.
- `/contacts` agenda without a create button: SDD-011 defines the agenda as overview + detail;
  contacts are created inline where they are named, and SDD-016's create-prompt rule applies only
  where a record can be created.
- `dotnet r ci` vs github.md's "nobody runs a local gate": availability is not obligation; no
  change.

## Suggested follow-up issues (not this review)

1. Per-tenant storage quota: one account can fill the shared `files` volume and the database
   (100 MB × unlimited uploads, unbounded comment bodies) — product decision.
2. Network-level guarantee for `BehindReverseProxy=true` (compose-internal network or an
   authenticated proxy hop), so "unreachable except through the proxy" is enforced, not assumed.
3. `TabBlazor 0.15.48-beta`: a pre-release UI dependency in a production app; track upstream for
   a stable release.
