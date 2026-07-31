# AGENTS.md

Single source of truth for AI agent instructions in this repository. Other agent files (`CLAUDE.md`, `.cursor/rules/`) only point here.

Skills follow the same pattern: the canonical skill is `.claude/skills/<name>/SKILL.md`; `.cursor/skills/` and `.codex/skills/` hold pointer skills with identical frontmatter (the description drives auto-activation) and a one-line body pointing to the canonical file. Never duplicate skill content.

## Project overview

EvilCase is a case-file management system: users create case files that evolve over time with comments, attachments and AI-assisted documents. Current state is a proof-of-concept skeleton: ASP.NET Core API + Blazor WebAssembly frontend with one echo round-trip. .NET 10, PostgreSQL, secrets in a local `.env` file.

## Solution map

All code lives in `src/` (solution `EvilCase.slnx`).

| Project | Purpose |
| --- | --- |
| `Api/EvilCase.Api` | ASP.NET Core API (controllers, OpenAPI + Scalar in dev) |
| `Api/EvilCase.Api.Client` | Typed API client: HTTP clients generated from API controllers |
| `Api/EvilCase.Api.Contract` | Shared request/response contracts (DTOs only) |
| `App/EvilCase.App` | Blazor WebAssembly standalone frontend |
| `Common/EvilCase.Auth` | JWT bearer authentication |
| `Data/EvilCase.Data` | EF Core model + DbContext (PostgreSQL) |
| `Data/EvilCase.Data.Migrations` | EF Core migrations |
| `Tests/EvilCase.Tests` | Application tests (NUnit) |
| `Utils/EvilBrains.Secrets.Infisical` | Infisical configuration provider (kept, not wired up) |
| `Utils/EvilBrains.*` | Shared libraries (collections, cryptography, logging for the wire contract, ASP.NET Core and WebAssembly, custom analyzers EB0001–EB0004, API client generator + controller convention analyzers EB1001–EB1016) |

## Secrets

Every environment reads secrets from environment variables through the `AddEnvironmentVariables` provider `CreateBuilder` registers. Development additionally loads `src/Api/EvilCase.Api/.env` (gitignored, `.env.example` documents the keys) into the process environment, so there is one configuration path everywhere and the file only decides where the values come from — hence the double underscore separator (`A__B` → `A:B`).

`DotNetEnv` does the loading, in `Program.cs`, with three constraints:

- It runs **before** `CreateBuilder`. That call is where `AddEnvironmentVariables` snapshots the process environment; anything set afterwards is invisible to configuration.
- `NoClobber()` — an environment variable that is already set wins over the file, matching how `.env` behaves everywhere else. This is why a `.env` cannot silently override what a container or CI job passes in.
- `TraversePath()` — the file is searched for upwards from `AppContext.BaseDirectory`, because `dotnet run` keeps the caller's working directory and it therefore cannot be found relative to it.

Because the check runs before the builder exists, `builder.Environment.IsDevelopment()` is unavailable and `ASPNETCORE_ENVIRONMENT` is read directly. Consequence: `dotnet run --environment X` does not affect it, only the variable does.

`EvilBrains.Secrets.Infisical` still holds the Infisical provider, but nothing calls it and `appsettings.json` no longer has the section it binds, so it needs that section back before it can be used.

## API client pattern

API controllers are the single source of truth; DTOs live in `EvilCase.Api.Contract`. `EvilCase.Api.Client` has no dependency on `EvilCase.Api`: it includes the controller sources as `AdditionalFiles` and the `EvilBrains.ApiClient.Generator` source generator emits clients from them (in-memory, never committed). Controllers marked `[GenerateApiClient]` (from `EvilBrains.ApiClient`) produce a public `I{Name}Client` interface, an internal implementation and a DI registration; consumers register clients via `Bootstrap.AddEvilCaseApiClient` from `EvilCase.Api.Client`, which takes an optional `Action<IHttpClientBuilder>` so message handlers attach to the generated clients only.

Controller conventions, enforced by analyzers in the API project (EB1001–EB1005) and re-checked by the generator with exact file/line locations:

- Every controller declares `[Route]` and every action exactly one HTTP method attribute with a route template (empty `""` allowed). Templates never start with `/` (controller and action templates are joined and the leading slash is implicit) and contain no `[controller]`/`[action]` tokens; literal segments are snake_case.
- Every action parameter carries exactly one binding attribute (`[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromHeader]`, `[FromServices]`, ...); `CancellationToken` carries none.

Client generation rules (EB1010–EB1016, generator-only): actions return `void`, `T`, `Task`/`ValueTask` or `Task<T>`/`ValueTask<T>`, optionally wrapped in `ActionResult`/`ActionResult<T>`/`IActionResult` — the generated client method is always asynchronous and an untyped result becomes a `Task` without a value (non-success status codes throw `ApiException`). Parameter and return types must be resolvable in the client compilation (Contract or shared libs), `[FromServices]`/`[FromKeyedServices]` parameters are omitted from the client, a complex `[FromQuery]` DTO is expanded property-by-property into query parameters (camelCase keys, simple-typed properties only), `[FromForm]`/`IFormFile` are unsupported.

## Logging

The mechanics live in three Utils libraries; the apps only wire them up. `EvilBrains.Logging.Contract` holds the wire contract (client log DTOs, header and property names), `EvilBrains.Logging.AspNetCore` the server half, `EvilBrains.Logging.WebAssembly` the browser half.

Every event carries `AppSource`, either `Client` or `Server`. The API enriches its own events with `Server`; `ClientLogWriter` puts `Client` on the events it rebuilds, which wins because properties on an event beat enrichers, and the name is reserved so a browser entry cannot claim to be a server one.

API: Serilog is configured in `Program.cs` from the `Serilog` configuration section (console everywhere, Seq per environment) and handed to `UseSerilog(Log.Logger)` — the parameterless overload registers no `Serilog.ILogger`, which `AddClientLogWriter` needs. Log call sites go through `[LoggerMessage]` partial methods — CA1848 runs at error severity. `Bootstrap` passes the source context browser logs are recorded under, `Program.cs` calls `app.UseRequestLogging("/logs/client")`.

Frontend: `EvilCase.App` uses Serilog as well, with the differences WebAssembly forces:

- There is no host, so `builder.Host.UseSerilog()` does not exist. `AddClientLogging` builds the logger and registers it with `builder.Logging.ClearProviders()` + `AddSerilog` (from `Serilog.Extensions.Logging`, not `Serilog.AspNetCore`).
- `Serilog.Settings.Configuration` is not used: it resolves sinks by assembly name through reflection, which breaks under WASM trimming. Levels are bound from the `ClientLogging` section of `wwwroot/appsettings.json` — `MinimumLevel` for the browser console, `ServerMinimumLevel` for the events shipped to the API. The two are independent: the pipeline threshold is the more verbose of them and each destination restricts itself, so either one can be the looser. The logger exists before the container does, so the section is bound directly with `Get<ClientLoggingOptions>()` instead of `IOptions<T>`; `EnableConfigurationBindingGenerator` on the library keeps that binding trim-safe — the property belongs to the project holding the call site. The bound properties are `get; set;`: the generated binder assigns after construction and silently skips `init`-only properties, which leaves the defaults in place with no error anywhere.
- Browser console output goes through `Serilog.Sinks.BrowserConsole`, which uses real console levels instead of stdout.
- `ClientLogSink` buffers events (500 max, then drops) and posts them to `POST /logs/client` every second in batches of at most 100. Events keep their structure: the sink ships the unrendered message template plus at most 16 properties, values rendered to strings and capped at 512 characters. A failed batch is dropped and the failure goes to Serilog's `SelfLog`; logging it normally would feed the sink that just failed.
- `AddRequestLogging()` replaces the factory's own HTTP logging (`RemoveAllLoggers()` + an `IHttpClientLogger`), because its four events per request use a template that cannot be changed and carry none of the request identifiers. `ClientHttpLogger` writes one event per request, `HTTP {HttpMethod} {RequestPath} responded {StatusCode} in {Elapsed} ms`, with `RequestId` and `CorrelationId` read back from the headers the handler stamped. A successful request to the upload path is not logged at all: the next upload would ship that log and log again. A failed one is, and it settles because a batch that fails is dropped rather than retried.
- The sink knows no API client: `EvilCase.App`'s `ApiLogUploader` implements `IClientLogUploader` over the generated `ILogsClient` and translates its transport failures into `ClientLogUploadException`, which is the only exception the sink swallows.
- The sink is created before the host exists, so `host.StartClientLogging()` hands it the uploader after `builder.Build()`. Forgetting that call is silent: events buffer and are dropped at 500.

`ClientLogWriter` on the API rebuilds a Serilog `LogEvent` from the entry. The endpoint is anonymous, so the whole payload is hostile input:

- The parsed template is the allow-list of property names — a property the template does not reference is dropped, and so is anything in `ReservedLogPropertyNames`. Properties carried on an event win over enrichers, so a client must not be able to name one.
- `MessageTemplateParser.Parse` throws on alignments that overflow; the failure falls back to logging the raw text. An alignment wider than 64 stays unbound, because padding is only rendered for bound properties.
- Control characters are stripped from the template, property values, category and URL — the plain text console sink is otherwise forgeable. The exception text keeps them.
- The event timestamp is the server clock; the browser value is kept as `ClientTimestamp`. Browser clocks are arbitrary and would corrupt the Seq timeline.
- The browser exception text arrives as a `ClientLogException`.

Request context: `AddRequestContextHeaders()` on a generated client stamps every request with `X-Request-Id` (fresh per request), `X-Correlation-Id` (same value), `X-Session-Id` (one GUID per app load) and `X-Machine-Id`. The machine identifier lives in `localStorage` under `evilcase.machine-id` and survives reloads and browser restarts; `ClientIdentity` reads it through synchronous WebAssembly interop, which is what makes it available to the handler. On the server `UseRequestLogging` validates the headers as GUIDs, re-formats them and pushes them into the Serilog `LogContext`, so every event of that request carries them, and only then runs Serilog's request logging — that ordering is why the two are one call. They land under `XRequestId`, `XCorrelationId`, `XSessionId` and `XMachineId` — the prefix keeps them together when a log store sorts properties by name, and keeps them clear of `RequestId`, which ASP.NET Core owns: it opens a scope per request whose `RequestId` is the `TraceIdentifier`, and a scope property reaches the event ahead of the log context, so a shared name would leave everything logged through `ILogger<T>` carrying the connection-local identifier while Serilog's own completion event carried the caller's. The middleware pushes the trace identifier under `RequestId` as well, because that scope does not reach Serilog's completion event. Successful log uploads, successful health probes and successful `OPTIONS` requests are demoted to `Verbose`, so none of them leaves a completion log.

An entry written while an API call was in flight carries that call's `RequestId` and `CorrelationId`, which the writer validates and puts on the event, so the browser side of a request and its server side share an identifier. Entries written outside a call — a component logging before or after `await` — have none and inherit the identifiers of the upload that carried them; correlate those through `SessionId` and `MachineId` plus `ClientUrl` and `ClientTimestamp`.

Seq credentials stay on the server; the browser only ever talks to the API.

## Health checks

Two anonymous endpoints, mapped with `MapHealthChecks` in `Program.cs` rather than through a controller — they carry no client contract, so they stay out of OpenAPI, out of the generated API client and out of the EB1001–EB1005 controller conventions. Both carry `AllowAnonymous`: nothing requires authentication today, but an authorization fallback policy would otherwise turn every probe into a `401` and take all instances out of rotation.

- `GET /health/live` runs no check (`Predicate = _ => false`) and answers `Healthy` as plain text. A dependency check here would restart every instance at once on a brief database outage.
- `GET /health/ready` runs the checks tagged `HealthCheckTags.Ready` and writes names and statuses as JSON. Each layer registers its own checks — `EvilCase.Data` contributes `AddEvilCaseDataHealthChecks`, today a single `AddDbContextCheck<ApplicationDbContext>` (`CanConnectAsync`) named `database` — and the API only decides which tags a probe runs. `HealthCheckResponseWriter` keeps descriptions, exception text and check data out of the response because the endpoint is anonymous. Status codes: 200 healthy, 503 unhealthy and 503 degraded — the last one overrides the default 200, which would keep an instance in rotation on a partial failure.

`/health` is a quiet path of `UseRequestLogging`, so probes leave no log unless they fail. It is also excluded from `UseHttpsRedirection` — orchestrators send probes over plain HTTP by default and a redirect carries no body, so it counts as a failed probe.

## Frontend UI

`EvilCase.App` builds on [TabBlazor](https://github.com/TabBlazor/TabBlazor) (Blazor components over the Tabler CSS framework).

- Package: `TabBlazor`. Services registered with `AddTabBlazor()` in `Program.cs`; `@using TabBlazor` in `_Imports.razor`. `index.html` must link `EvilBrains.EvilCase.App.styles.css` — several TabBlazor components (tooltip, dropdown, datepicker, popover, range slider) ship their CSS as Blazor scoped styles and silently render unstyled without it. `TabBlazor.QuickTable.EntityFramework` belongs to the API project, not here — the frontend talks to the API, never to the database.
- Tabler CSS is vendored at `wwwroot/lib/tabler/tabler.min.css` (Tabler core 1.4.0, matching the TabBlazor release). No CDN at build or runtime. Update by downloading the matching Tabler version.
- Popper is enabled (`DefaultPositioning = Absolute`), so dropdowns, tooltips and typeaheads flip away from viewport edges. popper.js 2.11.8 is vendored at `wwwroot/lib/popper/popper.min.js` and `TablerOptions.PopperScriptUrl` points there; the default would load it from unpkg.
- TabBlazor ships no icon set. `Icons/AppIcons.cs` holds only the icons the app uses; add one when it is needed by copying its path data from the [Tabler icon set](https://tabler.io/icons) into a new `TablerIcon`. Do not vendor the whole generated set — it is 5665 icons the trimmer does not remove.
- App shell: `Layout/MainLayout.razor` (Tabler `page` + `page-wrapper`) and `Layout/NavMenu.razor` — a single top bar holding the brand, the menu (from `lg` up) and the theme switch. Below `lg` the hamburger opens the navigation as an offcanvas via `IOffcanvasService`. Both render the same `Layout/NavLinks.razor`.
- `NavLinks` sets `active` on the `li`, not through Blazor's `NavLink`: Tabler draws the active indicator on `.nav-item.active`, an underline in the horizontal menu and a left border in the offcanvas.
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

  Both variants render. At 25–50 rows per page the cost is negligible and it avoids JS interop and prerender flicker.
- Never branch layout in C# by viewport. No JS interop for window width, no render branching on it: on Blazor Server every resize is a network roundtrip, prerendering does not know the viewport and flickers, and it does not work before Blazor boots.
- Modals: always `class="modal-fullscreen-md-down"`.
- Dates: native `<input type="date">`, no JS datepicker.
- Keyboard: set `inputmode` and `type` per input kind (`numeric` for case numbers, `tel`, `email`).
- Touch targets: at least 44px for interactive elements below `lg`.
- Forms: action buttons (Save/Cancel) sticky at the bottom, not hidden below long content.
- Safe area: `env(safe-area-inset-bottom)` on fixed bottom elements.
- Tooltips are never the only carrier of information — touch has no hover.
- No Bootstrap JS components; they collide with the Blazor renderer. Use TabBlazor equivalents (`IModalService`, `IOffcanvasService`). Where JS is unavoidable, wrap it in an `IJSObjectReference` cleaned up in `IAsyncDisposable`.
- Custom CSS stays minimal and lives in `wwwroot/css/app.css`. Look for a Tabler utility class first. No inline styles.

## Conventions

- Respond in the language of the user's message.
- Everything committed to the repo is English only: code, comments, documentation, AI instructions, commit messages, merge request descriptions, routes and URLs. Exception: user-facing UI strings are Czech.
- All written texts (docs, AI instructions, READMEs): concise and factual. State what, not why. No filler.
- Code style: clean, readable code sometimes beats 100% correctness and defensiveness.
- Every class resolved from DI is `internal sealed` and is consumed through an interface; when the consumer is public (a controller, a public extension method) the interface is public and the implementation stays internal. Exceptions are types the framework instantiates by concrete type or that have no service role: controllers, `DelegatingHandler` subclasses, middleware, exceptions, DTO and options records, static helpers.
- Comments only when something is unexpected (e.g. a workaround). If code needs a comment, prefer rewriting the code to be more readable.
- Analyzers run at error severity (Meziantou, Roslynator, custom EvilBrains). Fix findings, do not suppress without reason.
- Package versions belong only in `src/Directory.Packages.props` (Central Package Management).
- Namespaces/assemblies are auto-prefixed to `EvilBrains.*` by `src/Directory.Build.props`.
- One type per file.

## Commands

Run everything from `src/`:

- `dotnet r build` — build solution (Release, warnings as errors)
- `dotnet r test` — run tests
- `dotnet r format` / `dotnet r format-check` — format / verify formatting
- `dotnet r ci` — format-check + build + test
- `dotnet r run-api` — run API at `https://localhost:5000` (Scalar UI at `/scalar` in dev)
- `dotnet r run-app` — run frontend at `https://localhost:5001`
- `dotnet r add-migration` / `remove-migration` / `generate-sql-script` — EF migrations
