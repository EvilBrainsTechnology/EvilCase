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

Client generation rules (EB1010–EB1016, generator-only): actions return `void`, `T`, `Task` or `Task<T>`, optionally wrapped in `ActionResult`/`ActionResult<T>`/`IActionResult` — the generated client method is always asynchronous and an untyped result becomes a `Task` without a value (non-success status codes throw `ApiException`). Parameter and return types must be resolvable in the client compilation (Contract or shared libs), `[FromServices]`/`[FromKeyedServices]` parameters are omitted from the client, a complex `[FromQuery]` DTO is expanded property-by-property into query parameters (camelCase keys, simple-typed properties only), `[FromForm]`/`IFormFile` are unsupported.

## Conventions

- Respond in the language of the user's message.
- Everything committed to the repo is English only: code, comments, documentation, AI instructions, commit messages, merge request descriptions.
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
