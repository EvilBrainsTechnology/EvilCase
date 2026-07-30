# AGENTS.md

Single source of truth for AI agent instructions in this repository. Other agent files (`CLAUDE.md`, `.cursor/rules/`) only point here.

Skills follow the same pattern: the canonical skill is `.claude/skills/<name>/SKILL.md`; `.cursor/skills/` and `.codex/skills/` hold pointer skills with identical frontmatter (the description drives auto-activation) and a one-line body pointing to the canonical file. Never duplicate skill content.

## Project overview

EvilCase is a case-file management system: users create case files that evolve over time with comments, attachments and AI-assisted documents. Current state is a proof-of-concept skeleton: ASP.NET Core API + Blazor WebAssembly frontend with one echo round-trip. .NET 10, PostgreSQL, secrets via Infisical.

## Solution map

All code lives in `src/` (solution `EvilCase.slnx`).

| Project | Purpose |
| --- | --- |
| `Api/EvilCase.Api` | ASP.NET Core API (controllers, OpenAPI + Scalar in dev) |
| `Api/EvilCase.Api.Client` | Typed API client: HTTP clients generated from API controllers |
| `Api/EvilCase.Api.Contract` | Shared request/response contracts (DTOs only) |
| `App/EvilCase.App` | Blazor WebAssembly standalone frontend |
| `Common/EvilCase.Auth` | JWT bearer authentication |
| `Common/EvilCase.Secrets` | Infisical configuration provider |
| `Data/EvilCase.Data` | EF Core model + DbContext (PostgreSQL) |
| `Data/EvilCase.Data.Migrations` | EF Core migrations |
| `Tests/EvilCase.Tests` | Application tests (NUnit) |
| `Utils/EvilBrains.*` | Shared libraries (collections, cryptography, logging, custom analyzers EB0001–EB0004, API client generator + controller convention analyzers EB1001–EB1016) |

## API client pattern

API controllers are the single source of truth; DTOs live in `EvilCase.Api.Contract`. `EvilCase.Api.Client` has no dependency on `EvilCase.Api`: it includes the controller sources as `AdditionalFiles` and the `EvilBrains.ApiClient.Generator` source generator emits clients from them (in-memory, never committed). Controllers marked `[GenerateApiClient]` (from `EvilBrains.ApiClient`) produce a public `I{Name}Client` interface, an internal implementation and a DI registration; consumers register clients via `Bootstrap.AddEvilCaseApiClient` from `EvilCase.Api.Client`.

Controller conventions, enforced by analyzers in the API project (EB1001–EB1005) and re-checked by the generator with exact file/line locations:

- Every controller declares `[Route]` and every action exactly one HTTP method attribute with a route template (empty `""` allowed). Templates never start with `/` (controller and action templates are joined and the leading slash is implicit) and contain no `[controller]`/`[action]` tokens; literal segments are snake_case.
- Every action parameter carries exactly one binding attribute (`[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromHeader]`, `[FromServices]`, ...); `CancellationToken` carries none.

Client generation rules (EB1010–EB1016, generator-only): actions return `void`, `T`, `Task`/`ValueTask` or `Task<T>`/`ValueTask<T>`, optionally wrapped in `ActionResult`/`ActionResult<T>`/`IActionResult` — the generated client method is always asynchronous and an untyped result becomes a `Task` without a value (non-success status codes throw `ApiException`). Parameter and return types must be resolvable in the client compilation (Contract or shared libs), `[FromServices]`/`[FromKeyedServices]` parameters are omitted from the client, a complex `[FromQuery]` DTO is expanded property-by-property into query parameters (camelCase keys, simple-typed properties only), `[FromForm]`/`IFormFile` are unsupported.

## Frontend UI

`EvilCase.App` builds on [TabBlazor](https://github.com/TabBlazor/TabBlazor) (Blazor components over the Tabler CSS framework).

- Package: `TabBlazor`. Services registered with `AddTabBlazor()` in `Program.cs`; `@using TabBlazor` in `_Imports.razor`. `index.html` must link `EvilBrains.EvilCase.App.styles.css` — several TabBlazor components (tooltip, dropdown, datepicker, popover, range slider) ship their CSS as Blazor scoped styles and silently render unstyled without it. `TabBlazor.QuickTable.EntityFramework` belongs to the API project, not here — the frontend talks to the API, never to the database.
- Tabler CSS is vendored at `wwwroot/lib/tabler/tabler.min.css` (Tabler core 1.4.0, matching the TabBlazor release). No CDN at build or runtime. Update by downloading the matching Tabler version.
- Popper is enabled (`DefaultPositioning = Absolute`), so dropdowns, tooltips and typeaheads flip away from viewport edges. popper.js 2.11.8 is vendored at `wwwroot/lib/popper/popper.min.js` and `TablerOptions.PopperScriptUrl` points there; the default would load it from unpkg.
- TabBlazor ships no icon set. `Icons/AppIcons.cs` holds only the icons the app uses; add one when it is needed by copying its path data from the [Tabler icon set](https://tabler.io/icons) into a new `TablerIcon`. Do not vendor the whole generated set — it is 5665 icons the trimmer does not remove.
- App shell: `Layout/MainLayout.razor` (Tabler `page` + `page-wrapper`) and `Layout/NavMenu.razor` — a single top bar holding the brand, the menu (from `lg` up) and the theme switch. Below `lg` the hamburger opens the navigation as an offcanvas via `IOffcanvasService`. Both render the same `Layout/NavLinks.razor`.
- `NavLinks` sets `active` on the `li`, not through Blazor's `NavLink`: Tabler draws the active indicator on `.nav-item.active`, an underline in the horizontal menu and a left border in the offcanvas.
- Dark/light switch goes through `TablerService.SetTheme`.

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
- Everything committed to the repo is English only: code, comments, documentation, AI instructions, commit messages, merge request descriptions. Exception: user-facing UI strings are Czech.
- All written texts (docs, AI instructions, READMEs): concise and factual. State what, not why. No filler.
- Code style: clean, readable code sometimes beats 100% correctness and defensiveness.
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
- `dotnet r add-secret` — set a user secret for the API
