# EvilCase

A case-file system for administrative and legal proceedings: a case nests into sub-cases to any
depth and accumulates acts, parties, file marks, tags and comments. Built so far — the domain
model in PostgreSQL, authentication, and a Blazor WebAssembly frontend whose case screens are
still placeholders; `docs/product/vision.md` is what the rest is built towards.

.NET 10, PostgreSQL, secrets from environment variables.

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
| `Utils/EvilBrains.*` | Shared libraries: collections, cryptography, EF Core helpers, logging (`Logging.Contract`, `Logging.AspNetCore`, `Logging.WebAssembly`), API client attributes, the API client source generator, the custom analyzers and an unwired Infisical configuration provider |
| `Utils/Tests/EvilBrains.Utils.Tests` | Tests for the shared libraries |

One process serves everything: `/api/**` is the API, every other path returns the frontend. The
dependency runs host → api → business → data, and host → app → api client → contract;
`.claude/rules/business.md` holds the layering and names the test that pins it.

## Where the instructions live

The rules are in `.claude/rules/` and load themselves — always, or when a file in their area is
read. Detail needed only occasionally sits next to what it describes and is referenced from the
rules: `docs/product/vision.md` (the product, the domain concepts, the milestones),
`deploy/README.md` (image, registry tags, compose stack), the two logging READMEs under
`src/Utils/`, `.claude/skills/run-app/SKILL.md` (running and verifying the app) and
`.claude/skills/product-loop/SKILL.md` (the unattended loop, entry point `.claude/loop.md`).

## Commands

Run everything from `src/`. `r` is a local tool, so `dotnet tool restore` is required once per
clone.

- `dotnet r build` — build solution (Release, warnings as errors)
- `dotnet r test` — run tests
- `dotnet r format` / `dotnet r format-check` — format / verify formatting
- `dotnet r ci` — format-check + build + test
- `dotnet r run` — run everything at `https://localhost:5000` (Scalar UI at `/scalar` in dev); requires a reachable PostgreSQL
- `dotnet r add-migration` / `remove-migration` / `generate-sql-script` — EF migrations

A reachable PostgreSQL and a seeded administrator are the two prerequisites that are not in the
solution; `.claude/skills/run-app/SKILL.md` has the whole sequence.
