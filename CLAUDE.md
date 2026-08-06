# EvilCase

A case-file system for administrative and legal proceedings; `docs/product/vision.md` is the
product it is built towards.

.NET 10, PostgreSQL, secrets from environment variables. `src/global.json` pins the SDK version
and `.claude/hooks/session-start.sh` hardcodes its image tag — a bump changes both, in the same
commit.

## Solution map

All code lives in `src/` (solution `EvilCase.slnx`).

| Project | Purpose |
| --- | --- |
| `EvilCase.Host` | The only runnable project: composition root, serves the API and the frontend |
| `Api/EvilCase.Api` | ASP.NET Core API as a library (controllers, health checks, OpenAPI + Scalar in dev) |
| `Api/EvilCase.Api.Client` | Typed API client: HTTP clients generated from API controllers |
| `Api/EvilCase.Api.Contract` | Shared request/response contracts (DTOs only) |
| `App/EvilCase.App` | Blazor WebAssembly frontend |
| `Business/EvilCase.Business` | Business logic: the rules, the queries that answer a screen, the services the API calls |
| `Business/EvilCase.Domain` | The shared kernel: the enums an entity and a wire DTO both name. No dependencies at all |
| `Common/EvilCase.Auth` | Authentication: JWT bearer, sign-in, refresh token sessions, lockout, seeding |
| `Data/EvilCase.Data` | EF Core model + DbContext (PostgreSQL) — schema, nothing else |
| `Data/EvilCase.Data.Migrations` | EF Core migrations |
| `Tests/EvilCase.Tests` | Application tests (NUnit), including the host's routing through `WebApplicationFactory` |
| `Utils/EvilBrains.*` | Shared libraries: collections, cryptography, EF Core helpers, logging, the API client source generator and the custom analyzers |
| `Utils/Tests/EvilBrains.Utils.Tests` | Tests for the shared libraries |

One process serves everything: `/api/**` is the API, every other path returns the frontend. The
dependency runs host → api → business → data, and host → app → api client → contract;
`.claude/rules/business.md` holds the layering and names the test that pins it.

## Where the instructions live

The rules are in `.claude/rules/` and load themselves — always, or when a file in their area is
read. Detail sits next to what it describes: `docs/product/vision.md`, `deploy/README.md`, the
two logging READMEs under `src/Utils/`, and the `run-app` and `product-loop` skills.

## Commands

Run everything from `src/`. `r` is a local tool, so `dotnet tool restore` is required once per
clone.

- `dotnet r build` — build solution (Release, warnings as errors)
- `dotnet r test` — run tests
- `dotnet r format` / `dotnet r format-check` — format / verify formatting
- `dotnet r ai-check` — verify the AI instruction length limits
- `dotnet r ci` — the four above in one command, for CI. A pull request runs them one at a time
  (`.claude/rules/github.md`). Iterate on `dotnet r build` and `dotnet r test -- --no-build
  --filter FullyQualifiedName~<type>`.
- `dotnet r run` — run everything at `https://localhost:5000` (Scalar UI at `/scalar` in dev); requires a reachable PostgreSQL
- `dotnet r run-docker` — the same, in Docker with its own PostgreSQL, for a person trying the application out
- `dotnet r add-migration` / `remove-migration` / `generate-sql-script` — EF migrations

A reachable PostgreSQL and a seeded administrator are the two prerequisites `dotnet r run` needs
and the solution does not carry; `README.md` has both.
