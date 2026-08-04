# AI instructions audit

Inventory of every rule in the repository's AI instructions, the target structure they move into,
and the length limits that keep them small. Written for the refactor tracked on branch
`claude/ai-instructions-audit-refactor-dxp6x7`; the *Disposition* column is updated as rules move.

## 1. The loop

Task: run one round of the EvilCase product loop per firing. Entry point `.claude/loop.md`,
specification `.claude/skills/product-loop/SKILL.md`, fired by an hourly server-side Routine bound
to the session. A round: apply answered decision issues → clear every open pull request (answer
review threads, fix red CI, rebase stale bases, fix drifted descriptions) → take the
highest-value unblocked issue, preferring one that lands on `master` alone → open decision issues
for product questions and continue with what is not blocked → ship thin vertical slices
(database to UI) until nothing is tractable → report once, in Czech, Prague times.

Constraints:

- Never pushes to `master`; merges only what the owner has approved, by the protocol in
  `github` (before the refactor it never merged at all — see the §Phase 5 rows).
- Definition of done: `dotnet r ci` green, new tests, visual proof screenshots, docs updated in
  the same commit. A red gate is fixed, never suppressed; a slice that cannot meet it shrinks.
- With two or more open pull requests it opens nothing new (exception: owner-requested work).
- Asks the owner via decision issues plus a short chat question; never blocks waiting.
- Work runs in worktree-isolated subagents; the main thread keeps only reports, the schedule and
  owner communication. Anything that must survive a round is written to the repository or GitHub.
- GitHub via `curl` + `$GH_TOKEN` (no `gh`, MCP tools not guaranteed in fired rounds).
- The repository is public: no real case data anywhere.
- Ends only when the owner says so, or when everything is blocked (said once, out loud).

## 2. Inventory

One row per rule. Class: **binding** (a rule to keep), **fact** (describes the app or the
environment, not conduct), **duplicate** (stated elsewhere too), **dead** (state, history, or no
longer true). Disposition names the target file, or says why the rule is deleted. `writing`,
`code`, `github`, `instructions`, `api`, `app`, `business`, `data`, `auth` are files under
`.claude/rules/`.

### CLAUDE.md (root, 80 lines)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| C1 | Project description, solution map table | fact | stays (trimmed) |
| C2 | Dependency direction: host → api → business → data, never api → app, never api → data | binding | one line stays; detail in `business` |
| C3 | Table of per-area instruction files, read the one you touch, do not restate its rules elsewhere | fact | replaced by `.claude/rules/` path scoping; the do-not-restate clause moves to `instructions` |
| C4 | A new project with rules of its own gets its own instruction file | binding | `instructions` |
| C5 | Implementation detail lives in a README next to the code | binding | `instructions` |
| C6 | Respond in the language of the user's message | binding | `writing` |
| C7 | Everything committed is English; UI strings are Czech | binding | `writing`, widened to every GitHub write (the loop skill's clause) |
| C8 | Written texts concise and factual; state what, not why; no filler | binding | superseded by `writing` (Phase 3) |
| C9 | Commits and PR descriptions open with a TL;DR | binding | `writing` |
| C10 | Commit during the work, one logical unit per commit | binding | `github` |
| C11 | An agent never merges on its own initiative, never pushes `master`; owner's explicit request required; no auto-merge, no protection changes | binding | replaced by the merge protocol in `github` (Phase 5: owner approves via review, agent merges) |
| C12 | Clean readable code sometimes beats 100% correctness and defensiveness | binding | `code` |
| C13 | No `Async` suffix; two named exceptions | binding | `code` |
| C14 | DI classes `internal sealed` behind an interface; named exceptions | binding | `code` |
| C15 | Comments only for the unexpected; prefer rewriting the code | binding | `writing` |
| C16 | Rationale belongs in the commit/PR, never in code | binding | `writing` |
| C17 | A comment is one or two lines; no `<remarks>` essays, no copied `<inheritdoc/>` | binding | `writing` (merged with C15) |
| C18 | A test's assertion message names the broken rule, in a clause | binding | `code` |
| C19 | Analyzers at error severity; fix findings, do not suppress without reason | binding | `code` |
| C20 | `ILogger` called directly with a constant template; CA1848 off, no `[LoggerMessage]` | binding | `code` |
| C21 | Package versions only in `src/Directory.Packages.props` | binding | `code` |
| C22 | Namespaces/assemblies auto-prefixed `EvilBrains.*` | fact | `code` (one line) |
| C23 | One type per file | binding | `code` |
| C24 | Commands: `dotnet r build/test/format/ci/run`, migrations; run from `src/` | fact | stays |
| C25 | Prerequisites (PostgreSQL, seeded admin) live in the run-app skill | fact | stays (pointer) |

### src/Api/CLAUDE.md (64)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| A1 | Controller `[Route]` templates carry the `api/` prefix themselves; analyzer-enforced | binding | `api` |
| A2 | Unmatched `/api` answers problem-details 404, never the app's HTML; test-pinned | binding | `api` |
| A3 | Controllers live in a library, registered via `AddApplicationPart` | fact | deleted: visible in `Program.cs` |
| A4 | `EvilCase.Api` has no Web SDK implicit usings; import per file | fact | deleted: the compiler reports it |
| A5 | Same-origin only, no CORS; frontend takes base address from `HostEnvironment.BaseAddress` | binding | `api` |
| A6 | `BehindReverseProxy` / `HttpsRedirection` keys and their proxy-trust consequences | fact | one line in `api`; detail moved to `deploy/README.md` |
| A7 | Security headers from `SecurityHeadersMiddleware`; CSP names every inline-script hash; `/scalar` excluded | binding | `api` |
| A8 | Anonymous auth endpoints rate-limited per caller, limiter after forwarded headers, before authentication; health probes never limited | binding | `api` (condensed) |
| A9 | `PrincipalOwnerContext` implements `IOwnerContext` from the `sub` claim | duplicate (B15) | `business` owns it; dropped here |
| A10 | API clients generated from controller sources (`AdditionalFiles`); client never references `EvilCase.Api`; `[GenerateApiClient]`; register via `AddEvilCaseApiClient` | fact | `api` (condensed) |
| A11 | Generated routes are relative; base address normalised to end in `/` | fact | `api` (one line) |
| A12 | `EB1001`–`EB1016` are the controller/client spec; read the diagnostic, never work around it; no `[FromForm]`/`IFormFile` | binding | `api` |
| A13 | Health endpoints mapped anonymously outside controllers; `/health/live` never checks a dependency; responses never carry exception detail | binding | `api` |
| A14 | Per-layer health check chain, host names the tag | duplicate (B7) | `business` owns it; dropped here |
| A15 | Secrets from environment variables; dev loads `.env`; DotNetEnv before `CreateBuilder`, `NoClobber()`, `TraversePath()` must not change | binding | `api` |
| A16 | `ASPNETCORE_ENVIRONMENT` read before the builder; `--environment` has no effect | fact | `api` (clause on A15) |
| A17 | Infisical provider exists unwired | fact | deleted: already in root solution map |
| A18 | `AppSource` reserved; request logging is an allow-list, never a deny-list; `ClientLogRoute` named once; Seq keys and credential placement | binding | moved to `src/Utils/EvilBrains.Logging.AspNetCore/README.md`; `api` keeps the read-the-READMEs pointer |

### src/App/CLAUDE.md (49)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| P1 | `index.html` must link the app's scoped-styles bundle or TabBlazor components silently lose CSS | binding | `app` |
| P2 | Tabler CSS and popper.js vendored at matching versions; no CDN | binding | `app` |
| P3 | `AppIcons` holds only used icons; never vendor the whole set | binding | `app` |
| P4 | Shell layout structure; `active` set on the `li`, not via `NavLink` | fact | `app` (one line) |
| P5 | Theme via `TablerService.SetTheme`; changing an inline script changes its CSP hash | binding | `app` |
| P6 | A new page inside `MainLayout` is authenticated automatically | duplicate (U6) | `app` keeps the page-placement half; `auth` owns default deny |
| P7 | `host.StartClientLogging()` mandatory after `Build()`; forgetting is silent | binding | `app` (one line; mechanics in the logging README) |
| P8 | Desktop primary; mobile fully usable for reading and quick flows, must-not-break for admin flows; named screen lists | binding | `app` (condensed) |
| P9 | One breakpoint: `lg` (992px), never mixed with `md` | binding | `app` |
| P10 | Data lists never scroll horizontally; render both variants, switch by CSS only; `Home.razor` is the reference | binding | `app` (example dropped, reference kept) |
| P11 | Never branch layout in C# or JS by viewport | binding | `app` |
| P12 | Modals always `modal-fullscreen-lg-down` | binding | `app` |
| P13 | Dates: native `<input type="date">`, no JS datepicker | binding | `app` |
| P14 | `inputmode`/`type` per input kind | binding | `app` |
| P15 | Touch targets ≥ 44px below `lg` | binding | `app` |
| P16 | Form action buttons sticky at the bottom | binding | `app` |
| P17 | `env(safe-area-inset-bottom)` on fixed bottom elements | binding | `app` |
| P18 | Tooltips never the only carrier of information | binding | `app` |
| P19 | No Bootstrap JS; TabBlazor services; unavoidable JS via `IJSObjectReference` disposed in `IAsyncDisposable` | binding | `app` |
| P20 | Custom CSS minimal, in `app.css`; Tabler utility first; no inline styles | binding | `app` |

### src/Business/CLAUDE.md (36)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| B1 | Business logic only in `EvilCase.Business`; the frontend never decides | binding | `business` |
| B2 | `EvilCase.Domain` references nothing | binding | `business` |
| B3 | `EvilCase.Data` is schema only; no contract reference | binding | `business` |
| B4 | Business owns rules and queries; a query composes and materialises in one place | binding | `business` |
| B5 | A business service returns the contract DTO; no second model set, no mapping layer | binding | `business` |
| B6 | `EvilCase.Api` is HTTP only; a controller never sees `DbContext` or `IQueryable` | binding | `business` |
| B7 | Health checks chain per layer; the host calls the top and names the tag | binding | `business` (condensed) |
| B8 | `EvilCase.Auth` is a closed module behind `IAuthService`, exempt from the layering | fact | `business` (one line) |
| B9 | `LayerTests` pin every arrow | fact | `business` (one line) |
| B10 | Pure rules are static classes with no `DbContext`, tested without one | binding | `business` |
| B11 | List queries: one `IQueryable` step per rule, composed by a reader; nothing materialises early | binding | `business` |
| B12 | The projection selects straight into the contract DTO | binding | `business` |
| B13 | Search terms escaped for `ILIKE`; case folding never via `ToLower()` | binding | `business` |
| B14 | List queries tested via `ToQueryString()`, no server | binding | `business` |
| B15 | `IOwnerContext` is the only ownership seam; never a threaded `ownerId`, never `HttpContext` | binding | `business` |
| B16 | `OwnerId` throws, `OwnerIdOrDefault` is for callers where absence is normal | binding | `business` |
| B17 | A future tenant lands in this seam | fact | `business` (one line) |

### src/Data/CLAUDE.md (30)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| D1 | The type is `Case`; `@case` where the keyword collides; CA1716 at `suggestion` | binding | `data` |
| D2 | Every aggregate root carries `OwnerId` from its first migration, FK + index | binding | `data`; the loop-skill copy drops, vision keeps it as product context |
| D3 | Nesting is a self-reference; deleting a case cascades to its sub-tree | fact | `data` (one line) |
| D4 | Tree walks are pure over navigations and carry a visited set | binding | `data` (one line) |
| D5 | Enums stored as names, `HasConversion<string>()` with explicit length | binding | `data` |
| D6 | Tags are rows, typed, unique per case — never an array column | binding | `data` |
| D7 | File mark vs file number semantics (spisová značka / číslo jednací) | duplicate | `docs/product/vision.md` owns the domain concepts; dropped here |
| D8 | The case's own mark is a required unique column; external marks are rows with a required assigning party | fact | `data` (one line) |
| D9 | An act's ordinal is deliberately not unique; never key on `(CaseId, Ordinal)` | binding | `data` |
| D10 | Dates a period runs from are `DateOnly`; timestamps stay `DateTime` | binding | `data` |
| D11 | FKs to `Parties` are `Restrict`; the owning case cascades | binding | `data` |
| D12 | A file asset is bytes only; name, role and origin act live on the link | duplicate | vision owns the concept; dropped here |
| D13 | `FileAssets` unique on `(OwnerId, ContentHash)`, never the hash alone | binding | `data` |
| D14 | Comments: one table, two nullable parents, XOR check constraint; check constraints read from the design-time model in tests | binding | `data` |
| D15 | `MigrateOnStartup` semantics and when to turn it off | duplicate | `deploy/README.md` and root README own it; dropped here |
| D16 | `dotnet ef migrations add` builds without warnings-as-errors; run a non-incremental build after adding one | binding | `data` |
| D17 | Migrations regenerate via remove + add, never re-add over a committed snapshot; verify with `generate-sql-script` | binding | `data` |
| D18 | Runtime registration and the design-time factory both call `UseEvilCaseMigrations` | binding | `data` |

### src/Common/EvilCase.Auth/CLAUDE.md (25)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| U1 | Token architecture: 15-minute HS256 access token in memory, hashed refresh token in `__Host` cookie, rotation spends atomically, 30 s race grace, 14/30-day lifetimes, lockout 5×/15 min | fact | `auth` (condensed to the non-obvious invariants) |
| U2 | `AuthSessionId`, never `SessionId` — everywhere | binding | `auth` |
| U3 | CSRF defence is `SameSite=Strict` + same-origin; no antiforgery token | fact | `auth` (one line) |
| U4 | Registration closed; seed creates the first administrator only into an empty table | fact | `auth` (one line) |
| U5 | Default deny via fallback policy; the `[AllowAnonymous]` list is test-pinned | binding | `auth` |
| U6 | Adding `[AllowAnonymous]`, or a page outside `MainLayout`, is an owner decision | binding | `auth` (page half also in `app`, see P6) |
| U7 | Browser half: token store, renewal a minute early, one 401 retry copying options and body, `IAuthSession` resolved on use, only the three anonymous endpoints skip renewal | fact | `auth` (condensed) |

### src/EvilCase.Host/CLAUDE.md (7)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| H1 | Pointer: host rules live in `src/Api/CLAUDE.md` | fact | deleted: `api` scopes to `src/Api/**` and `src/EvilCase.Host/**` via `paths` |

### .claude/loop.md (20)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| L1 | Run one round; the skill is the specification, this file only the entry point | binding | stays |
| L2 | The round's five-step order | duplicate (S16–S25) | deleted: the skill owns the order |
| L3 | The round runs in a subagent; the main thread relays and confirms the schedule | duplicate (S9, S38) | deleted |
| L4 | A round ends when nothing is left to move; report once | duplicate (S2) | deleted |
| L5 | Never merge, never push to `master` | duplicate (C11) | deleted: `github` is always loaded |

### .claude/skills/product-loop/SKILL.md (267)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| S1 | Never ask permission to continue | binding | stays |
| S2 | A round runs until nothing is tractable, then reports everything once | binding | stays |
| S3 | Read vision, root instructions and the open work at round start | binding | stays as vision + open work; the rule files load themselves |
| S4 | GitHub via `curl` + `$GH_TOKEN`; no `gh`; never depend on MCP tools in a round | binding | stays |
| S5 | Worktrees refuse pipelines: write scripts to the scratchpad; parse with `python3` | binding | stays (condensed) |
| S6 | REST endpoint reference table | fact | moved to `.claude/skills/product-loop/github-api.md` |
| S7 | The loop's pull requests are the `loop/*` and `claude/*` branches, not an author | binding | replaced: Phase 5 — the loop tends **every** open pull request |
| S8 | The loop's own comments are recognised by author `claude[bot]`, measured not assumed | binding | stays |
| S9 | Work runs in subagents; the main thread delegates and keeps the report | binding | stays |
| S10 | A task is delegated whole, through to the published pull request | binding | stays |
| S11 | Owner communication and the schedule stay in the main thread | binding | stays (the merge clause is replaced by `github`) |
| S12 | Independent tasks run in parallel; every writing subagent gets a worktree | binding | stays |
| S13 | Worktree caveats: parent's root instructions, no `.env` | fact | one line; `.env` half owned by run-app skill |
| S14 | A pull request is not a workbench; verify with tools | binding | stays |
| S15 | The repository is the only memory: findings → issues, PR state → PR comments, rules → instruction files | binding | stays |
| S16 | Answered decisions: `## Decision` comment, label `decided`, close, unblock referencing issues; vision updated with the code it governs, alone when it governs none | binding | stays |
| S17 | Outstanding = a thread with no `claude[bot]` reply; never filter by timestamp or count | binding | stays (anecdote dropped) |
| S18 | Clear every open pull request before taking new work; every comment answered in the round it is found | binding | stays |
| S19 | A loop pull request needs explicit approval; `mergeable_state: blocked` is the protection rule | binding | replaced by the merge protocol in `github` |
| S20 | Pick the highest-value unblocked issue; lands-on-`master` first, lowest milestone second; no milestone defers nothing; honour a focus argument | binding | stays |
| S21 | Empty backlog → derive the next slice from the vision, open its issue, take it | binding | stays |
| S22 | Everything blocked → one chat message re-surfacing the questions, stop the round | binding | stays |
| S23 | Ask generously: every product/domain/UX branch gets a decision issue plus a short chat question; issue format defined | binding | stays (format condensed) |
| S24 | Technical choices covered by instructions are made silently | binding | stays |
| S25 | One thin vertical slice per PR; branch `loop/<issue>-<slug>`; base = `master` or the branch it builds on | binding | stays |
| S26 | The `CLAUDE.md` files are binding, without exception | fact | deleted: instructions bind by definition |
| S27 | Every new endpoint authenticated, every new page in `MainLayout`; `[AllowAnonymous]` is a decision issue | duplicate (U5, U6) | `auth` owns default deny; `api` and `app` each carry their half at the point of use |
| S28 | Every new aggregate root carries its owner from the first migration | duplicate (D2) | deleted: `data` owns it |
| S29 | Definition of done: `dotnet r ci`, new tests, visual proof, docs in the same commit | binding | stays |
| S30 | A red gate is fixed, never worked around; shrink the slice if it cannot pass | binding | stays |
| S31 | Visual proof: run the app, sign in, screenshot changed screens at 1440×900 and 390×844; commit under `docs/screenshots/<issue>/`; embed by commit-pinned raw URL | binding | gate stays; mechanics moved to `.claude/skills/product-loop/visual-proof.md` |
| S32 | Playwright module/browser paths; never `playwright install` | fact | moved to `visual-proof.md` |
| S33 | `docs/screenshots/` is not on `master` yet — #102 creates it | dead | deleted: #102 merged |
| S34 | A rebase orphans raw URLs — re-point in the same step; check with `curl` and no auth header | binding | moved to `visual-proof.md` |
| S35 | GitHub sometimes wraps a written URL in backticks; read the body back, fall back to `<img>` (#102 history) | binding | moved to `visual-proof.md` (history dropped) |
| S36 | Superseded screenshots deleted in the same PR | binding | moved to `visual-proof.md` |
| S37 | PR body: TL;DR, what changed, screenshots, `Closes #`; then `subscribe_pr_activity` | binding | stays (TL;DR clause deferred to `writing`) |
| S38 | Schedule: an hourly session-bound Routine, never `CronCreate`; arm at session start; repair in place, delete only a duplicate or the ended loop; a denied Routine tool is the owner's question; every turn ends confirming it; cadence named in the report | binding | stays (condensed, anecdotes dropped) |
| S39 | Watch pull requests via `subscribe_pr_activity`; re-subscribe at session start; unsubscribe on close | binding | stays |
| S40 | Stop after opening the PR; a round is the agent's own initiative, so the merge exception never applies | binding | replaced by the merge protocol in `github` |
| S41 | Report: one Czech message, Prague times, everything the round did, what waits, what is next; never a question | binding | stays |
| S42 | The loop ends only when the owner says so or by the §3 stop | binding | stays |
| S43 | Stacks: base = the head below; prefer landing on `master`; shorten from the bottom; a stack is a cost | binding | stays (condensed) |
| S44 | Stack REST API; `unstack` destroys without confirmation; merged members stay listed | fact | moved to `github-api.md` |
| S45 | A merge moves the floor: retarget and rebase what remains | binding | stays (feeds the Phase 5 merge protocol) |
| S46 | An agent does not decide a stack is ready; merging the bottom is still merging | binding | replaced by the merge protocol in `github` |
| S47 | Real case folders are read-only reference; no real case content, names, marks or personal data in the public repository | binding, duplicate (vision Privacy) | `github` owns the rule; vision keeps the product context |
| S48 | On a usage limit wait and resume; never trade the definition of done for tokens | binding | stays |
| S49 | Prefer reversible steps; destructive/dependency/auth/security changes are a decision issue first | binding | stays |
| S50 | A round is bounded by the work, not a count; clearing what is open is unbounded | binding | stays (merged with S2) |
| S51 | Each pull request stays one thin phone-reviewable slice | binding | stays |
| S52 | With ≥ 2 open pull requests, open nothing new; owner-requested work excepted | binding | stays |
| S53 | The loop never waits for the owner; the only stop is §3's | binding | stays |

### .claude/skills/run-app/SKILL.md (79)

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| R1 | Prerequisites: tool restore, `.env` with seed + JWT, reachable PostgreSQL, dev cert | fact | stays (trimmed) |
| R2 | The session-start hook restates environment facts; changing any of them changes the hook in the same commit | binding | stays |
| R3 | A worktree has no `.env`; copy it or run from the main checkout | fact | stays |
| R4 | Start through the `evilcase` preview server; one instance per port; keep the port off the unsafe list | binding | stays |
| R5 | Verification sequence: health, sign-in, echo round-trip, 404 shape, frontend | fact | stays (trimmed) |

### Other files

| # | Rule | Class | Disposition |
| --- | --- | --- | --- |
| O1 | `.claude/commands/loop-bootstrap.md` — one-off setup procedure | fact | stays; loads only when invoked, outside the limit set; its endpoint list now points at `github-api.md` |
| O2 | `.claude/settings.json` — Routine-tool permissions, session-start hook | fact | stays (configuration, not prose) |
| O3 | `.claude/hooks/session-start.sh`, `.claude/launch.json` | fact | stays (code/configuration) |
| O4 | `.github/workflows/*.yml`, `dependabot.yml` — CI comments explain trigger choices | fact | stays; CI.yml gains the limits check (Phase 6) |
| O5 | `README.md`, `deploy/README.md`, logging READMEs, `test-data/README.md` | fact | stay; README/CLAUDE.md overlap trimmed on the CLAUDE.md side |
| O6 | `docs/product/vision.md` — product, domain, milestones, privacy | fact | stays; privacy rule moves to `github`, vision keeps the context |

### Rules that exist nowhere yet (Phase 5)

| # | Rule | Disposition |
| --- | --- | --- |
| N1 | All work through pull requests from branches; never push to `master` | `github` |
| N2 | Commits and pull requests are authored as `claude[bot]` | `github` |
| N3 | PR title and description always match the current diff; update on every content change | `github` |
| N4 | The loop tends every open pull request: review comments, replies, rebases, green CI | `github` + loop skill |
| N5 | The agent merges, the owner only approves; merge order and inter-merge rebasing are the agent's | `github` |
| N6 | Merge only with the owner's `APPROVED` review, green CI and no conflicts; squash; delete the branch | `github` |
| N7 | Never approve your own PR; never bypass the review gate (no admin merge, no force push to `master`) | `github` |

## 3. Target structure

```
CLAUDE.md                      map: what the app is, solution table, commands, pointers
.claude/rules/
  writing.md                   style of every produced text (Phase 3)
  instructions.md              how instructions change; the length ratchet (Phase 4)
  github.md                    branches, commits, PR lifecycle, merge protocol, privacy (Phase 5)
  code.md        paths: src/** — C# conventions
  api.md         paths: src/Api/**, src/EvilCase.Host/**
  app.md         paths: src/App/**
  business.md    paths: src/Api/**, src/Business/**, src/Data/** — the layering governs all three
  data.md        paths: src/Data/**
  auth.md        paths: src/Common/EvilCase.Auth/**
.claude/skills/product-loop/SKILL.md     the loop, tightened
.claude/skills/product-loop/github-api.md    REST reference (loads on demand)
.claude/skills/product-loop/visual-proof.md  screenshot mechanics (loads on demand)
.claude/skills/run-app/SKILL.md          trimmed
.claude/loop.md                entry point, minimal
```

The six `src/**/CLAUDE.md` files are deleted; their rules live in the path-scoped files above,
which load when a matching file is read. `docs/**` and `README.md` files are unlimited and hold
what is needed only occasionally; instructions reference them.

## 4. Length limits

Counted as physical lines (`wc -l`). Scope — every location Claude Code loads as instructions:
`**/CLAUDE.md` (any directory), `.claude/CLAUDE.md`, `.claude/rules/**/*.md`,
`.claude/skills/**/SKILL.md`. Skill reference files, `docs/**` and READMEs are out of scope by
design: they load on demand. `@path` imports would load uncounted lines, so instruction files
use none (`.claude/rules/instructions.md`).

Before the refactor the scoped files held 637 lines; after it they hold 516.

| Limit | Value |
| --- | --- |
| Per file | 120 lines |
| Sum over all scoped files | 650 lines |

The binding case for 120 is the product-loop skill; every other file lands well under it. Both
limits are a ratchet: lowering is allowed, raising is not. When a change would exceed a limit,
shorten elsewhere. CI enforces both from one configuration file
(`.claude/instruction-limits.json`).
