# EvilCase

A case-file system for administrative and legal proceedings; `docs/product/vision.md` is the
product it is built towards.

.NET 10, PostgreSQL, secrets from environment variables. `src/global.json` pins the SDK version
and `.claude/hooks/session-start.sh` hardcodes its image tag — a bump changes both, in the same
commit.

## Solution map

All code lives in `src/` (solution `EvilCase.slnx`). `EvilCase.Host` is the only runnable
project: one process serves `/api/**` and returns the frontend on every other path.
`Api/EvilCase.Api` is the API as a library, `Api.Client` the typed client generated from the
controllers, `Api.Contract` the shared DTOs. `App/EvilCase.App` is the Blazor WebAssembly
frontend. `Business/EvilCase.Business` holds the business logic, `Business/EvilCase.Domain` the
dependency-free shared kernel, `Common/EvilCase.Auth` authentication. `Data/EvilCase.Data` is
the EF Core model (PostgreSQL), schema only; `Data.Migrations` its migrations.
`Tests/EvilCase.Tests` holds the application tests (NUnit), `Utils/EvilBrains.*` the shared
libraries and their tests.

The rules are in `.claude/rules/` and load themselves — always, or when a file in their area is
read. Detail sits next to what it describes: `docs/product/vision.md`, `docs/sdd/`,
`deploy/README.md`, the `src/Utils/` logging READMEs, and the `run-app` and `product-loop` skills.

## Commands

Run everything from `src/`; `r` is a local tool, `dotnet tool restore` once per clone.

- `dotnet r build` — build solution (Release, warnings as errors)
- `dotnet r test` — run tests; iterate with `dotnet r test -- --no-build --filter FullyQualifiedName~<type>`
- `dotnet r format` / `format-check` — format / verify formatting
- `dotnet r ai-check` — verify the AI instruction length limits
- `dotnet r ci` — the four above in one command
- `dotnet r run` — run at `https://localhost:5000` (Scalar at `/scalar` in dev); needs a reachable PostgreSQL and a seeded administrator — `README.md` has both
- `dotnet r run-docker` — the same in Docker with its own PostgreSQL
- `dotnet r add-migration` / `remove-migration` / `generate-sql-script` — EF migrations
