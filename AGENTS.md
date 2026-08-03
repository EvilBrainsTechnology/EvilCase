# AGENTS.md

Single source of truth for AI agent instructions in this repository. Other agent files (`CLAUDE.md`, `.cursor/rules/`) only point here.

Skills follow the same pattern: the canonical skill is `.claude/skills/<name>/SKILL.md`; `.cursor/skills/` and `.codex/skills/` hold pointer skills with identical frontmatter (the description drives auto-activation) and a one-line body pointing to the canonical file. Never duplicate skill content.

Implementation detail lives in a README next to the code it describes, so it changes in the same commit as the code:

- `src/Utils/EvilBrains.Logging.AspNetCore/README.md` — server-side logging
- `src/Utils/EvilBrains.Logging.WebAssembly/README.md` — browser-side logging
- `deploy/README.md` — image, registry tags and the compose stack

## Project overview

EvilCase is a case-file management system: users create case files that evolve over time with comments, attachments and AI-assisted documents. Current state is a proof-of-concept skeleton: ASP.NET Core API + Blazor WebAssembly frontend with one echo round-trip, served by a single host. .NET 10, PostgreSQL, secrets in a local `.env` file.

## Solution map

All code lives in `src/` (solution `EvilCase.slnx`).

| Project | Purpose |
| --- | --- |
| `EvilCase.Host` | The only runnable project: composition root, serves the API and the frontend |
| `Api/EvilCase.Api` | ASP.NET Core API as a library (controllers, health checks, OpenAPI + Scalar in dev) |
| `Api/EvilCase.Api.Client` | Typed API client: HTTP clients generated from API controllers |
| `Api/EvilCase.Api.Contract` | Shared request/response contracts (DTOs only) |
| `App/EvilCase.App` | Blazor WebAssembly frontend |
| `Common/EvilCase.Auth` | Authentication: JWT bearer, sign-in, refresh token sessions, lockout, seeding |
| `Data/EvilCase.Data` | EF Core model + DbContext (PostgreSQL) |
| `Data/EvilCase.Data.Migrations` | EF Core migrations |
| `Tests/EvilCase.Tests` | Application tests (NUnit), including the host's routing through `WebApplicationFactory` |
| `Utils/EvilBrains.Secrets.Infisical` | Infisical configuration provider (kept, not wired up) |
| `Utils/EvilBrains.*` | Shared libraries: collections, cryptography, EF Core helpers, logging (`Logging.Contract`, `Logging.AspNetCore`, `Logging.WebAssembly`), API client attributes, the API client source generator and the custom analyzers |

## Hosting

One process serves everything. `EvilCase.Host` is the composition root: it owns `Program.cs`, the middleware pipeline and all configuration (`appsettings*.json`, `.env`, `launchSettings.json`). The dependency runs host → api, never api → app.

- `/api/**` is the API. Controller `[Route]` templates carry the prefix themselves — the client generator reads them from source and would not see a runtime routing convention — and an analyzer enforces it.
- An unmatched `/api` path is a `404` in problem details shape, from a fallback registered in `MapEvilCaseApi`, never the app's HTML: its literal segment gives it precedence over the catch-all `MapFallbackToFile("index.html")`. `Tests/EvilCase.Tests` pins that precedence through the real `Program.cs`.
- Everything else returns `index.html`, so client-side routes survive a reload. `/health/*`, `/scalar` and `/openapi/v1.json` are mapped explicitly and the fallback never reaches them.
- Controllers live in a library, so `AddControllers().AddApplicationPart(...)` registers them explicitly.
- `EvilCase.Api` is `Microsoft.NET.Sdk` + `FrameworkReference Microsoft.AspNetCore.App`, so it has none of the Web SDK's implicit usings — import ASP.NET Core namespaces per file.
- Same-origin: no CORS anywhere. The frontend takes its API base address from `builder.HostEnvironment.BaseAddress`.

Two keys under `EvilBrains:EvilCase:Hosting` adapt the pipeline to what sits in front of it: `BehindReverseProxy` (default `false`) calls `UseForwardedHeaders` first, with `KnownIPNetworks` and `KnownProxies` cleared and `ForwardLimit = 1`; `HttpsRedirection` (default `true`) turns `UseHttpsRedirection` off where something in front already redirects. `/health/*` is excluded from redirection either way. With no known proxy to check against, the single hop is the whole defence — a deployment that turns `BehindReverseProxy` on must not be reachable except through that proxy.

Baseline security headers, the content security policy among them, are written by `SecurityHeadersMiddleware`; `/scalar` is excluded, because the Scalar UI loads its bundle from a CDN. The policy has to name the hash of every inline script of `index.html`, which `SecurityHeadersTests` pins.

The anonymous entry points are rate limited per caller address, each in its own partition: `/api/auth/login` 5 per minute, `/api/auth/refresh` 60, the rest of `/api/auth/*` 10, the client log upload 120. Nothing else is limited, health probes above all. `UseRateLimiter` sits after `UseForwardedHeaders`, so a partition is the caller rather than the proxy, and ahead of `UseAuthentication`, so a rejected caller still spends permits.

## Authentication

Access token in memory, refresh token in a cookie. `EvilCase.Auth` holds all of it behind `IAuthService`; the controller only turns results into status codes and moves the cookie in and out.

- **Access token** — HS256 JWT, 15 minutes, returned in the response body and kept in the browser's memory only. Claims: `sub` (id), `unique_name` (e-mail, the name claim), `role`, `sid` (the session), `jti`. `MapInboundClaims` is off, so those are also the types on the principal; `AuthClaims` in `EvilCase.Api.Contract` names them for both halves.
- **`AuthSessionId`, never `SessionId`** — the identifier of a rotation chain, in the entity, the column, the contract and every log template. The logging pipeline already carries a browser session as `XSessionId` (`RequestContextPropertyNames.SessionId`), and one log event holds both.
- **Refresh token** — 32 bytes of randomness in `__Host-evilcase-refresh`: `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/`. Only its SHA-256 is stored. `SameSite=Strict` plus same-origin-only is the whole CSRF defence; there is no antiforgery token.
- **Rotation** — every refresh spends the token and issues another inside the same `SessionId`. Spending it is the atomic `UPDATE ... WHERE RevokedAt IS NULL`, not the read before it, so of two callers holding the same token exactly one is served. A token presented after it was revoked is a replay and ends that whole chain. Inside a 30-second grace window it is instead read as two tabs racing (`RefreshStatus.Raced`) and only refused — and the response leaves the cookie alone, because it already holds the winner's replacement and a delete matches by name.
- **Lifetimes** — a refresh token is good for 14 days, a rotation chain for 30 from sign-in whatever it does in between. Both under `EvilBrains:EvilCase:Auth:RefreshToken`.
- **Lockout** — 5 consecutive failures lock an account for 15 minutes (`Auth:Lockout`). The counter starts over with the lockout. Sign-in answers `401` for bad credentials and `423` for a lockout; the client branches on the status code and never on a message.
- **Seeding** — registration is closed and there is no register endpoint. `Auth:Seed` (e-mail and password) creates the first administrator at startup, only while the table holds no user at all. It never overwrites.
- **Default deny** — `AddEvilCaseAuth` sets an authorization fallback policy, so an endpoint with no attribute needs an authenticated caller. What stays open says so with `[AllowAnonymous]`: both `/health/*`, the API 404 fallback, `MapFallbackToFile("index.html")`, `/scalar` and `/openapi/v1.json`, `LogsController` (the frontend logs from the sign-in page too) and the sign-in, refresh and sign-out endpoints. `Tests/EvilCase.Tests/Hosting/AuthorizationFallbackTests` pins the list.

In the browser (`EvilCase.App/Auth`): `AccessTokenStore` holds the token in memory, `EvilCaseAuthenticationStateProvider` is both the state provider and `IAuthSession`, and `AuthTokenHandler` attaches the bearer, renews a minute before expiry and retries once after a `401`. Its first `GetAuthenticationStateAsync` calls refresh, which is what signs the user back in after a reload. The handler resolves `IAuthSession` on use rather than in its constructor — the renewal goes through a generated client that has the handler in its own chain. Only the three anonymous endpoints (`login`, `refresh`, `logout`) skip the renewal, because renewal itself goes through them; the `[Authorize]` ones under `/api/auth` are renewed like any other, or `logout-all` would fail silently on an expired token and leave every other device signed in. Paths are matched by segment, and everything under `/api/auth` is sent with `BrowserRequestCredentials.Include` — including the retry, which copies `HttpRequestMessage.Options` along with the headers and the buffered body.

The application is closed by default on the client too, and `MainLayout` is what does it: everything it lays out sits inside an `AuthorizeView`, so a new page is protected without doing anything. Escaping that means choosing another layout, which only `Pages/Login.razor` does (`LoginLayout`).

## Domain model

`docs/product/vision.md` is what the model is built towards and names the concepts; this section holds only what the code does differently or what a new entity has to repeat.

- **`Case`, and `@case` where it collides** — the domain's word is the code's word, so the type is `Case`, not a synonym invented to dodge a keyword. CA1716 flags identifiers matching a reserved word and is therefore set to `suggestion` in `src/.editorconfig`, the one rule deliberately below error. Where `case` would be a variable or lambda parameter, prefix it: `.Property(@case => @case.Status)`.
- **Every aggregate root carries `OwnerId` from its first migration**, with a foreign key to `Users` and an index. Nothing filters on it until M8 — until then a single user owns everything — and that is exactly why it has to be there from the start rather than be a data migration later.
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

Every aggregate root carries an `OwnerId` from its first migration. Nothing filters by it yet — until M8 a single user owns everything — and the seam that will is already in place.

- **`IOwnerContext` is the one place ownership is resolved.** `EvilCase.Data` declares it; `PrincipalOwnerContext` in `EvilCase.Api` implements it by reading the access token's `sub` claim, and is the only code in the application that reads that claim for this purpose. A query needing the owner takes `IOwnerContext`, never an `ownerId` parameter threaded down from a controller and never `HttpContext` of its own.
- **`OwnerId` throws, `OwnerIdOrDefault` does not.** Code that has no sensible behaviour without an owner takes the first: a query that would otherwise return another owner's rows, or silently none, is a bug either way. The second is for the callers where absence is normal — a health probe, the sign-in endpoint, a migration at startup.
- **This is where a tenant goes.** When the vision's multi-tenant horizon becomes real, an owner becomes a tenant and this interface is what changes, rather than every query in the application. That is the whole reason it exists before there is anything to filter.

## Secrets

Every environment reads secrets from environment variables. Development additionally loads `src/EvilCase.Host/.env` (gitignored, `.env.example` documents the keys) into the process environment, so there is one configuration path everywhere — hence the double underscore separator (`A__B` → `A:B`).

`DotNetEnv` does the loading, in `Program.cs`, with three constraints that must not be changed:

- It runs **before** `CreateBuilder`. That call is where `AddEnvironmentVariables` snapshots the process environment; anything set afterwards is invisible to configuration.
- `NoClobber()` — an environment variable that is already set wins over the file, so a `.env` cannot override what a container or CI job passes in.
- `TraversePath()` — the file is searched for upwards from `AppContext.BaseDirectory`, because `dotnet run` keeps the caller's working directory.

Because the check runs before the builder exists, `ASPNETCORE_ENVIRONMENT` is read directly rather than through `builder.Environment`. Consequence: `dotnet run --environment X` does not affect it, only the variable does.

`EvilBrains.Secrets.Infisical` still holds the Infisical provider, but nothing calls it and `appsettings.json` has no section for it.

## Database migrations

`Program.cs` awaits `MigrateEvilCaseDatabaseAsync` between `builder.Build()` and the middleware pipeline. A database that does not exist is created; an unreachable server stops the start, and there is no retry. `EvilBrains:EvilCase:Database:MigrateOnStartup` turns it off (default `true`) — do that where the schema is rolled out separately, or where several instances start at once, because nothing serialises concurrent migrators. `Tests/EvilCase.Tests` sets it to `false`.

**`dotnet ef migrations add` builds without `TreatWarningsAsErrors`**, unlike `dotnet r build`. A warning-level diagnostic in code written just before the migration — a `CS1574` cref that does not resolve, say — passes there, and the assembly it produces is then up to date, so later incremental builds skip recompiling it and never report the error. Run `dotnet build --no-incremental` (or `dotnet r ci` after touching the file again) once after adding a migration; a green `dotnet r build` straight afterwards proves nothing about the project the migration was generated from.

Migrations live in `EvilCase.Data.Migrations`, which references `EvilCase.Data` and cannot be referenced back. `UseEvilCaseMigrations` (in `EvilCase.Data`) names the assembly as a string and sets the `_MigrationsHistory` table name; EF loads the assembly at runtime and `EvilCase.Host` carries it into its output through an otherwise unused project reference. Both the runtime registration and `ApplicationDbContextFactory` must call that one extension — a mismatched history table makes EF re-apply every migration.

## Health checks

Two anonymous endpoints, mapped with `MapHealthChecks` in `MapEvilCaseApi` rather than through a controller: they carry no client contract, so they stay out of OpenAPI, out of the generated API client and out of the controller conventions. Keep `AllowAnonymous` on both — an authorization fallback policy would otherwise turn every probe into a `401`.

- `GET /health/live` runs no check (`Predicate = _ => false`) and answers `Healthy` as plain text. Never add a dependency check here.
- `GET /health/ready` runs the checks tagged `HealthCheckTags.Ready` and writes names and statuses as JSON: 200 healthy, 503 unhealthy, 503 degraded. Each layer registers its own checks — `EvilCase.Data` contributes `AddEvilCaseDataHealthChecks`, today a single `database` check.

`HealthCheckResponseWriter` keeps descriptions, exception text and check data out of the response, because the endpoint is anonymous.

## API client pattern

API controllers are the single source of truth; DTOs live in `EvilCase.Api.Contract`. `EvilCase.Api.Client` has no dependency on `EvilCase.Api`: it includes the controller sources as `AdditionalFiles` and the `EvilBrains.ApiClient.Generator` source generator emits clients from them, in memory, never committed. A controller marked `[GenerateApiClient]` produces a public `I{Name}Client` interface, an internal implementation and a DI registration. Consumers register clients via `Bootstrap.AddEvilCaseApiClient`, which takes an optional `Action<IHttpClientBuilder>` so message handlers attach to the generated clients only.

Generated routes are relative (`api/echo/post`, no leading slash) and resolve against the base address, which `AddEvilCaseApiClient` normalises to end in `/`. That is what keeps the app working when it is served from a sub-path.

Controller shape (route templates, HTTP method attributes, kebab-case segments, the `api/` prefix, parameter binding) and client feasibility (return types, parameter types, type visibility to the client compilation) are enforced at error severity with exact file and line locations — see *Conventions*. Read the diagnostic rather than working around it. `[FromForm]` and `IFormFile` are not supported.

## Logging

`EvilBrains.Logging.Contract` holds the wire contract (client log DTOs, header and property names); the server and browser halves are documented in their own READMEs, linked at the top of this file. Read those before changing anything in the logging pipeline.

Rules that hold outside those libraries:

- Every event carries `AppSource`, either `Client` or `Server`. The name is reserved: a browser entry cannot claim to be a server one.
- Request logging is an allow-list: `app.UseRequestLogging(loggedPaths: ["/api"], quietPaths: [ClientLogRoute.Path])`. Anything outside `loggedPaths` leaves no completion log unless it fails. Do not turn it into a deny-list — the host also serves the frontend and all its assets.
- The upload route is `ClientLogRoute` in `EvilCase.Api.Contract`, and the controller, the host's quiet path and the browser sink all take it from there. Naming it again anywhere breaks both feedback-loop guards silently.
- Seq is configured from `EvilBrains:EvilCase:Logging:Seq` (`ServerUrl`, `ApiKey`), not from the `Serilog` section, which only holds the console sink. The server URL is the only switch: an environment naming none logs to the console only.
- The `Environment` property is enriched from `builder.Environment.EnvironmentName`, never from an `appsettings.*.json` of its own.
- `host.StartClientLogging()` must be called after `builder.Build()` in `EvilCase.App`. Forgetting it is silent: browser events buffer and are dropped.
- Seq credentials stay on the server; the browser only ever talks to the API.

Log call sites call `ILogger` directly with a constant message template. CA1848 is off and `[LoggerMessage]` is not used.

## Frontend UI

`EvilCase.App` builds on [TabBlazor](https://github.com/TabBlazor/TabBlazor) (Blazor components over the Tabler CSS framework).

- Services registered with `AddTabBlazor()` in `Program.cs`; `@using TabBlazor` in `_Imports.razor`. `index.html` must link `EvilBrains.EvilCase.App.styles.css` — several TabBlazor components (tooltip, dropdown, datepicker, popover, range slider) ship their CSS as Blazor scoped styles and silently render unstyled without it. The host emits a bundle of its own next to it; the app's is the one to link.
- Tabler CSS is vendored at `wwwroot/lib/tabler/tabler.min.css` (Tabler 1.4.0, matching the TabBlazor release) and popper.js 2.11.8 at `wwwroot/lib/popper/popper.min.js`, which `TablerOptions.PopperScriptUrl` points at. No CDN at build or runtime. Update by downloading the matching version.
- TabBlazor ships no icon set. `Icons/AppIcons.cs` holds only the icons the app uses; add one by copying its path data from the [Tabler icon set](https://tabler.io/icons) into a new `TablerIcon`. Do not vendor the whole generated set — the trimmer does not remove it.
- App shell: `Layout/MainLayout.razor` (Tabler `page` + `page-wrapper`) and `Layout/NavMenu.razor` — a single top bar holding the brand, the menu (from `lg` up) and the theme switch. Below `lg` the hamburger opens the navigation as an offcanvas via `IOffcanvasService`. Both render the same `Layout/NavLinks.razor`, which sets `active` on the `li` rather than through Blazor's `NavLink`: Tabler draws the indicator on `.nav-item.active`.
- Dark/light switch goes through `TablerService.SetTheme`. The initial theme follows `prefers-color-scheme`: an inline script in `index.html` applies it before Blazor boots and `ThemeSwitch` reads it back via `wwwroot/js/theme.js`.

## Responsive design

Desktop is the primary channel. Mobile must be fully usable for reading and quick flows; for administrative flows it only must not break.

- First-class on mobile: case list + search, case detail, deadlines, quick note.
- Must not break on mobile: bulk operations, long edit forms, user management, configuration.

Rules:

- The desktop/mobile breakpoint is `lg` (992px). Use it consistently; do not mix `md` and `lg` across pages.
- Data lists never scroll horizontally on mobile. Render both variants and switch them with CSS only — see `Pages/Home.razor` for the reference implementation:

  ```razor
  <div class="d-none d-lg-block">
      <QuickTable Items="@items">...</QuickTable>
  </div>

  <div class="d-lg-none">
      @foreach (var item in items) { <ItemCard Item="item" /> }
  </div>
  ```

  Both variants render; at 25–50 rows per page the cost is negligible.
- Never branch layout in C# by viewport. No JS interop for window width, no render branching on it.
- Modals: always `class="modal-fullscreen-lg-down"`, matching the `lg` breakpoint above.
- Dates: native `<input type="date">`, no JS datepicker.
- Keyboard: set `inputmode` and `type` per input kind (`numeric` for case numbers, `tel`, `email`).
- Touch targets: at least 44px for interactive elements below `lg`.
- Forms: action buttons (Save/Cancel) sticky at the bottom, not hidden below long content.
- Safe area: `env(safe-area-inset-bottom)` on fixed bottom elements.
- Tooltips are never the only carrier of information — touch has no hover.
- No Bootstrap JS components; they collide with the Blazor renderer. Use TabBlazor equivalents (`IModalService`, `IOffcanvasService`). Where JS is unavoidable, wrap it in an `IJSObjectReference` cleaned up in `IAsyncDisposable`.
- Custom CSS stays minimal and lives in `wwwroot/css/app.css`. Look for a Tabler utility class first. No inline styles.

## Keeping the product loop running

The loop (`.claude/loop.md`, `.claude/commands/loop-step.md`) runs unattended, and the way it fails is by stopping without saying so. Its clock is therefore a **recurring schedule**, never a one-shot wake-up.

A one-shot wake-up has to be re-armed at the end of every turn, and *any* message from the owner in between is a turn. One such turn that ends without re-arming ends the loop silently: a stopped loop reports nothing, and nothing anywhere records that it was ever running. That is not hypothetical — it is how the loop died the first time, between a round and an unrelated question about pull requests.

- **The schedule is a Routine, hourly.** `create_trigger` bound to the session, and nothing else. It is stored server-side, so it is the only clock here that survives anything. Two of its properties are not negotiable and must not be fought: the minimum interval is one hour, and an hourly expression is re-anchored to the minute it was created in — asking for `0 * * * *` yields `N * * * *`. A cadence finer than hourly cannot be had, so do not promise one.
- **`CronCreate` does not work in Claude Code on the web.** Its jobs live in the session's memory, and this environment resumes the session between turns, which wipes them. Measured: three jobs created across one evening, none survived to its next fire, zero rounds ran. Use it only where a session is known to stay up, and never as the loop's only clock.
- **Watch the pull requests rather than poll them.** `subscribe_pr_activity` delivers comments, reviews and CI transitions straight into the session as `<github-webhook-activity>`, so a review comment is answered when it is written instead of at the next round. Subscribe to every pull request the loop opens, in the same step that opens it, and re-subscribe to the open ones at the start of a session — a subscription belongs to the session and does not outlive it. Unsubscribe when the pull request merges or closes. This is per pull request and covers nothing else: an answer on a decision issue is still found by the next round, which is what bounds how long one can wait.
- **A session that starts while the loop is meant to be running arms it.** `list_triggers` first: if no Routine exists for the loop, create one. `create_trigger` needs a permission the agent must not grant itself — `.claude/settings.json` allows it, and a denial there is a question for the owner rather than something to work around.
- **Every turn ends by confirming the schedule is still there** — including a turn that had nothing to do with the loop, and above all one that interrupted a round. `list_triggers` is the check; an empty answer while the loop is meant to be running means recreate it before the turn ends. This is a rule about turns, not rounds: the round ends when the report is written, the turn ends when the next round is scheduled.
- **A fired round may have no MCP tools.** A Routine carries no connector grants unless it was created by a session holding them, so a round can wake without `mcp__github__*`. Everything the loop needs from GitHub is therefore reachable another way: `curl` against `api.github.com` with `$GH_TOKEN`, which is in the environment, plus `git` itself. Never make a round depend on a tool it might not have.
- **The work runs in subagents, and a subagent starts blank.** Not only a round: review comments, an issue, an investigation. The main thread keeps the session — the pull request subscriptions and the conversation the owner reads — and delegates, so an hour of build logs never reaches the context that has to last. Independent tasks go out together, and every subagent that writes to the repository gets `isolation: "worktree"`, because one checkout shared between two of them loses work silently. The consequence is the rule: **the repository and GitHub are the only memory the loop has.** A finding becomes an issue, the state of a pull request its diff does not show becomes a comment on it, a rule becomes a line here. Something only the current chat knows is something the next round will not know, and the loop will not notice it forgot.
- **A pull request the loop opens needs an explicit approval; one the owner opened does not.** `gh` authenticates as the owner, `curl` with `$GH_TOKEN` as the `claude[bot]` app, and only the owner clears branch protection on `master`. So a green, answered, current pull request from the loop still sits at `mergeable_state: blocked` — that is the protection rule, not a defect, and the report says so rather than treating the branch as unready.
- **State the cadence in the report.** A loop that has quietly stopped should be visible in the chat, not only in the absence of anything happening.
- **Times are Prague time**, in the report and in every other message to the owner. The container runs UTC, so convert — `TZ=Europe/Prague date`.

The loop ends when the owner says so, or by the one genuine stop above — nothing buildable, said once. Both are stated out loud. A loop that stops any other way is a bug.

## Stacked pull requests

A slice that needs something not yet on `master` branches off the branch carrying it rather than off `master`, and its pull request targets that branch. Two or more such pull requests are linked as a **stack** on GitHub, so the chain is one object with an order rather than a set of pull requests that happen to point at each other.

**A stack is a cost, not an achievement.** It exists so that work can continue across a merge the agent is not the one to decide on — not so that more of it can be started. Everything in a stack is unreviewed, unmerged and rebasing itself every time the floor moves, and the deeper it goes the more of the repository lives in branches nobody has looked at. So: prefer a slice that lands on `master` on its own, take a layer only when the work genuinely cannot exist without an unmerged one, and treat an existing chain as something to shorten from the bottom rather than extend from the top. Getting what is already open into a state the owner can merge outranks starting anything new.

- **The base branch is the whole mechanism.** Each pull request's base must be the head of the one below it, bottom to top, the bottom on `master`. A diff then shows only what its own layer adds, which is the point of splitting the work.
- **Link the chain as soon as it is a chain.** A stack needs at least two pull requests; a slice that stands on its own opens an ordinary pull request against `master` and nothing more.
- **`gh stack` is the local tool** (`gh extension install github/gh-stack`) and is what to use on a workstation. In Claude Code on the web neither it nor `gh` is installed, and the egress policy blocks `github.com`, so use the REST API with `$GH_TOKEN` — `api.github.com` is reachable:

  | Call | What it does |
  | --- | --- |
  | `GET /repos/{owner}/{repo}/stacks` | The repository's stacks, newest first |
  | `GET /repos/{owner}/{repo}/stacks/{number}` | One stack |
  | `POST /repos/{owner}/{repo}/stacks` `{"pull_requests":[bottom,…,top]}` | Creates one; at least two numbers, and each base must match the previous head |
  | `POST /repos/{owner}/{repo}/stacks/{number}/add` `{"pull_requests":[…]}` | Appends to the top — the delta only, never the whole list |
  | `POST /repos/{owner}/{repo}/stacks/{number}/unstack` | **Dissolves the stack.** No body, no confirmation, and it answers `204` to a probe as readily as to an intention. Never call it to find out whether it exists. |

  A pull request carries its membership as a `stack` object (`number`, `size`, `position`), which is how to check a chain is linked without listing every stack.

- **A merge moves the floor.** When the bottom of a stack merges, GitHub retargets the one above it onto the merged base and the rest need rebasing onto the new `master`. That is work for whoever owns the branches, not a reason to touch the merge.
- **An agent does not decide a stack is ready**, and merging the bottom of one is still merging. The rule in *Conventions* holds here as it does everywhere: asked for a merge by name, an agent performs it; concluding on its own that the chain should land, never.

## Conventions

- Respond in the language of the user's message.
- Everything committed to the repo is English only: code, comments, documentation, AI instructions, commit messages, merge request descriptions, routes and URLs. Exception: user-facing UI strings are Czech.
- All written texts (docs, AI instructions, READMEs): concise and factual. State what, not why. No filler.
- Commit messages and merge request descriptions open with a TL;DR: one or two sentences saying what changed, before any detail.
- Commit during the work, not once at the end: every logical unit that stands on its own is its own commit, so a branch reads as a sequence of steps rather than one lump. That holds for agents too — do not wait for the whole task to be finished before the first commit.
- **An AI agent never merges a pull request on its own initiative, and never pushes to `master`.** Not because CI went green, not because the branch has waited, not because "finish" or "ship" sounds like it covers the merge. It merges when the owner asks for that merge in their own words, and then only what they named. A round of the loop, a webhook, a CI notification and the agent's own earlier plan are none of them such a request. Auto-merge is out either way — it is a merge decided now and performed later with nobody watching. So is a direct push to `master`, and so is any branch protection change that would allow one.
- Code style: clean, readable code sometimes beats 100% correctness and defensiveness.
- **No `Async` suffix on method names.** `IAuthService.Refresh`, not `RefreshAsync` — the return type already says it. Two exceptions: a genuine sync/async pair on the same surface, where the suffix is what tells them apart (`AsReadOnlyCollection` / `AsReadOnlyCollectionAsync` in `EvilBrains.Collections`), and members whose name is not ours to choose — `DelegatingHandler.SendAsync`, `IAsyncDisposable.DisposeAsync`, `ComponentBase.OnAfterRenderAsync` and the like.
- Every class resolved from DI is `internal sealed` and is consumed through an interface; when the consumer is public (a controller, a public extension method) the interface is public and the implementation stays internal. Exceptions are types the framework instantiates by concrete type or that have no service role: controllers, `DelegatingHandler` subclasses, middleware, exceptions, DTO and options records, static helpers.
- Comments only when something is unexpected (e.g. a workaround). If code needs a comment, prefer rewriting the code to be more readable.
- **Rationale for a change belongs in the commit message and the pull request, never in the code.** That is the rule agents break: a comment explaining *why this was done this way* is written for a reviewer who reads it once, and then stays in the file forever, restating a decision nobody is questioning any more. A comment is for the next reader of the code, and answers only *why does this look wrong when it is not*.
- **A comment is one or two lines.** If it needs a paragraph, a `<remarks>` block or a second sentence of argument, it is not a comment — it is a commit message in the wrong file. No essays on `<summary>`, no `<para>`, no restating what the signature already says: a member called `Acts` on a `Case` is documented by its name, and `/// <inheritdoc/>` copied onto a sibling property documents nothing.
- **A test's assertion message is not a place for prose either.** It says which rule broke, in a clause, not why the rule exists.
- Analyzers run at error severity (Meziantou, Roslynator, custom EvilBrains). Fix findings, do not suppress without reason. The custom rules are `EB0001`–`EB0004` (style), `EB1001`–`EB1006` (controller conventions, reported by analyzers in the API project and re-checked by the client generator) and `EB1010`–`EB1016` (client generation feasibility, reported by the generator only).
- Package versions belong only in `src/Directory.Packages.props` (Central Package Management).
- Namespaces/assemblies are auto-prefixed to `EvilBrains.*` by `src/Directory.Build.props`.
- One type per file.

## Commands

Run everything from `src/`. `r` is a local tool, so `dotnet tool restore` is required once per clone.

- `dotnet r build` — build solution (Release, warnings as errors)
- `dotnet r test` — run tests
- `dotnet r format` / `dotnet r format-check` — format / verify formatting
- `dotnet r ci` — format-check + build + test
- `dotnet r run` — run everything at `https://localhost:5000` (Scalar UI at `/scalar` in dev); requires a reachable PostgreSQL
- `dotnet r add-migration` / `remove-migration` / `generate-sql-script` — EF migrations

The database is the one prerequisite that is not in the solution. From the repository root, `docker compose -f deploy/docker-compose.dev.yml up -d --wait` starts a throwaway PostgreSQL on the connection string `.env.example` already carries; `deploy/README.md` documents it. Running the application also needs the seeded administrator (`EvilBrains__EvilCase__Auth__Seed__Email` and `__Password`), because there is no other way in. The `run-app` skill has the whole sequence, including how to verify a run.

In Claude Code on the web none of that is in the container. `.claude/hooks/session-start.sh` installs the .NET SDK and `pwsh`, starts PostgreSQL, writes `EvilCase.Host/.env` and restores, at session start and only where `CLAUDE_CODE_REMOTE` is `true`. It restates what lives elsewhere — the SDK version pinned in `global.json` and its `scriptShell`, the connection string and the seed and JWT keys of `.env.example`, the PostgreSQL version and the `src/` layout — so a change to any of those is a change to the hook, in the same commit. The `run-app` skill documents what it does and where it deviates from a local setup.

`launchSettings.json` holds a second profile, `claude`, identical to `https` except that it launches no browser; `.claude/launch.json` runs it. Both serve `https://localhost:5000`, so there is one address in every command, README and skill. The cost is that the two cannot run at once — stop the IDE instance before an agent starts one, or the second gets a port collision. When changing that port, keep it off the browsers' unsafe-port list (6000, 6665–6669, 6697, ...) — the preview pane refuses to load those.
