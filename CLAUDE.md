# EvilCase

A case-file system for administrative and legal proceedings: a case nests into sub-cases to any depth and accumulates acts, parties, file marks, tags and comments. Built so far — the domain model in PostgreSQL, authentication, and a Blazor WebAssembly frontend whose case screens are still placeholders; `docs/product/vision.md` is what the rest is built towards.

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
| `Common/EvilCase.Auth` | Authentication: JWT bearer, sign-in, refresh token sessions, lockout, seeding |
| `Data/EvilCase.Data` | EF Core model + DbContext (PostgreSQL) |
| `Data/EvilCase.Data.Migrations` | EF Core migrations |
| `Tests/EvilCase.Tests` | Application tests (NUnit), including the host's routing through `WebApplicationFactory` |
| `Utils/EvilBrains.*` | Shared libraries: collections, cryptography, EF Core helpers, logging (`Logging.Contract`, `Logging.AspNetCore`, `Logging.WebAssembly`), API client attributes, the API client source generator, the custom analyzers and an unwired Infisical configuration provider |
| `Utils/Tests/EvilBrains.Utils.Tests` | Tests for the shared libraries |

One process serves everything: `/api/**` is the API, every other path returns the frontend. The dependency runs host → api → auth → data, and host → app → api client → contract. Never api → app.

## Where the rest of the instructions live

Each file below loads only when its area is touched. Read the one that covers what you are changing; do not restate its rules elsewhere.

| File | Covers |
| --- | --- |
| `src/Api/CLAUDE.md` | Hosting and the middleware pipeline (`src/EvilCase.Host/CLAUDE.md` points here), controller conventions, the generated API client and its `EB1001`–`EB1016` diagnostics, health checks, security headers, rate limits, secrets, logging |
| `src/App/CLAUDE.md` | TabBlazor and Tabler, icons, the app shell and theme, responsive design |
| `src/Data/CLAUDE.md` | Entities and domain model rules, `OwnerId` and the `IOwnerContext` seam, migrations |
| `src/Common/EvilCase.Auth/CLAUDE.md` | Tokens, rotation, lockout, seeding, default-deny authorization, the browser half |
| `docs/product/vision.md` | What the product is being built into, the domain concepts, the milestones |
| `.claude/skills/run-app/SKILL.md` | Running the app locally or in a web session, and verifying it |
| `.claude/skills/product-loop/SKILL.md` | Operating the unattended product loop; `.claude/loop.md` is its entry point |

Implementation detail lives in a README next to the code it describes, so it changes in the same commit as the code: `src/Utils/EvilBrains.Logging.AspNetCore/README.md` (server-side logging), `src/Utils/EvilBrains.Logging.WebAssembly/README.md` (browser-side logging), `deploy/README.md` (image, registry tags, the compose stack).

## Conventions

- Respond in the language of the user's message.
- Everything committed is English only: code, comments, documentation, AI instructions, commit messages, pull request descriptions, routes and URLs. Exception: user-facing UI strings are Czech.
- All written texts (docs, AI instructions, READMEs): concise and factual. State what, not why. No filler.
- Commit messages and pull request descriptions open with a TL;DR: one or two sentences saying what changed, before any detail.
- Commit during the work, not once at the end: every logical unit that stands on its own is its own commit. That holds for agents too — do not wait for the whole task to be finished before the first commit.
- **An AI agent never merges a pull request on its own initiative, and never pushes to `master`.** Not because CI went green, not because the branch has waited, not because "finish" or "ship" sounds like it covers the merge. It merges when the owner asks for that merge in their own words, and then only what they named. A webhook, a CI notification, a scheduled round and the agent's own earlier plan are none of them such a request. Auto-merge is out either way, and so is any branch protection change that would allow a direct push.
- Code style: clean, readable code sometimes beats 100% correctness and defensiveness.
- **No `Async` suffix on method names.** `IAuthService.Refresh`, not `RefreshAsync` — the return type already says it. Two exceptions: a genuine sync/async pair on the same surface, where the suffix is what tells them apart (`AsReadOnlyCollection` / `AsReadOnlyCollectionAsync` in `EvilBrains.Collections`), and members whose name is not ours to choose — `DelegatingHandler.SendAsync`, `IAsyncDisposable.DisposeAsync`, `ComponentBase.OnAfterRenderAsync` and the like.
- Every class resolved from DI is `internal sealed` and is consumed through an interface; when the consumer is public (a controller, a public extension method) the interface is public and the implementation stays internal. Exceptions are types the framework instantiates by concrete type or that have no service role: controllers, `DelegatingHandler` subclasses, middleware, exceptions, DTO and options records, static helpers.
- Comments only when something is unexpected (e.g. a workaround). If code needs a comment, prefer rewriting the code to be more readable.
- **Rationale belongs in the commit message and the pull request, never in the code.** A comment is for the next reader of the code and answers only *why does this look wrong when it is not*.
- **A comment is one or two lines.** No `<remarks>` essays, no `<para>`, no restating what the signature already says, no `/// <inheritdoc/>` copied onto a sibling property.
- **A test's assertion message says which rule broke, in a clause** — not why the rule exists.
- Analyzers run at error severity (Meziantou, Roslynator, custom EvilBrains `EB0001`–`EB0004`, `EB1001`–`EB1016`). Fix findings, do not suppress without reason.
- Log call sites call `ILogger` directly with a constant message template. CA1848 is off and `[LoggerMessage]` is not used.
- Package versions belong only in `src/Directory.Packages.props` (Central Package Management).
- Namespaces and assemblies are auto-prefixed to `EvilBrains.*` by `src/Directory.Build.props`.
- One type per file.

## Commands

Run everything from `src/`. `r` is a local tool, so `dotnet tool restore` is required once per clone.

- `dotnet r build` — build solution (Release, warnings as errors)
- `dotnet r test` — run tests
- `dotnet r format` / `dotnet r format-check` — format / verify formatting
- `dotnet r ci` — format-check + build + test
- `dotnet r run` — run everything at `https://localhost:5000` (Scalar UI at `/scalar` in dev); requires a reachable PostgreSQL
- `dotnet r add-migration` / `remove-migration` / `generate-sql-script` — EF migrations

A reachable PostgreSQL and a seeded administrator are the two prerequisites that are not in the solution; `.claude/skills/run-app/SKILL.md` has the whole sequence, for a workstation and for Claude Code on the web alike.
