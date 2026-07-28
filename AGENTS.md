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
| `Api/EvilCase.Api.Client` | Typed API client: Refit interfaces + shared request/response contracts |
| `App/EvilCase.App` | Blazor WebAssembly standalone frontend |
| `Common/EvilCase.Auth` | JWT bearer authentication |
| `Common/EvilCase.Secrets` | Infisical configuration provider |
| `Data/EvilCase.Data` | EF Core model + DbContext (PostgreSQL) |
| `Data/EvilCase.Data.Migrations` | EF Core migrations |
| `Tests/EvilCase.Tests` | Application tests (NUnit) |
| `Utils/EvilBrains.*` | Shared libraries (collections, cryptography, logging, custom analyzers EB0001–EB0004) |

## API client pattern

`EvilCase.Api.Client` is the single source of truth for API contracts. Controllers implement the Refit interfaces; MVC routes are derived from the Refit attributes by `RefitRoutingApplicationModelProvider` — controllers must not carry routing attributes. Interface-routed controller actions return `Task<T>`, not `IActionResult`. Consumers register clients via `Bootstrap.AddEvilCaseApiClient` from `EvilCase.Api.Client` (the call must stay in that assembly so the generated-client module initializer runs).

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
