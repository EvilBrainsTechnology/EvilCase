# EvilCase

A case-file system for administrative and legal proceedings: a case accumulates acts, contacts, file marks, files and comments, and hangs under an optional parent case. Built so far — the domain model in PostgreSQL, authentication, and a Blazor WebAssembly frontend that ships the case list.

> **Proprietary — all rights reserved.** This repository is public to read, not to use. No right to run, copy, modify or distribute the software is granted; see [LICENSE.txt](LICENSE.txt) and ask before you use anything.

## Stack

.NET 10, PostgreSQL, Serilog. The frontend is Blazor WebAssembly on [TabBlazor](https://github.com/TabBlazor/TabBlazor) over the [Tabler](https://tabler.io) CSS framework, vendored — no CDN.

One process serves everything: `/api/*` goes to the API, every other path returns the WebAssembly app.

## Repository layout

All code lives in `src/` (solution `EvilCase.slnx`): `EvilCase.Host` is the only runnable project, `Api/` holds the API, its shared contracts and the generated client, `App/` the frontend, `Data/` the EF Core model and migrations, `Common/` and `Utils/` the shared libraries.

Full project map: [CLAUDE.md](CLAUDE.md); conventions: `.claude/rules/`. Deployment: [deploy/README.md](deploy/README.md).

## Local development

### Prerequisites

- .NET SDK per `src/global.json`
- A reachable PostgreSQL. The host migrates the database on startup and does not retry, so an unreachable server stops it. Set `EvilBrains__EvilCase__Database__MigrateOnStartup=false` to start without one. A throwaway one, matching the connection string in `.env.example`:

  ```
  docker compose -f deploy/docker-compose.dev.yml up -d --wait
  ```
- Trusted dev certificate: `dotnet dev-certs https --trust`

### Secrets

Secrets come from environment variables in every environment. In Development they are loaded from `src/EvilCase.Host/.env`, which is not committed.

- Copy `src/EvilCase.Host/.env.example` to `src/EvilCase.Host/.env` and fill in the values.
- Keys use the environment variable separator: `A__B` maps to the configuration key `A:B`.
- The file is read in the Development environment only, and only fills in what is not already set — an environment variable you export yourself wins over it.
- Deployed environments set the same keys as real environment variables and no file is involved.

### Build and run

From `src/`:

```
dotnet tool restore          # once per clone: `r` is a local tool
dotnet r build
dotnet r run                 # https://localhost:5000 (Scalar UI at /scalar)
```

Registration is closed, so signing in needs the administrator seeded from `EvilBrains__EvilCase__Auth__Seed__Email` and `EvilBrains__EvilCase__Auth__Seed__Password` — set both before the first start against an empty database. The seed also creates the account, the tenant and the administrator's default contact.

### Everything in Docker

`dotnet r run-docker` builds the image from this repository and runs it with its own PostgreSQL at `http://localhost:8080`, seeding `admin@evilcase.local` / `DevPassword123!`. Of the prerequisites above only the SDK is needed, plus Docker — no `.env`, no certificate, no database of one's own. Details in [deploy/README.md](deploy/README.md).

### Tests

```
dotnet r test                # tests only
dotnet r ci                  # format-check + build + test, what CI runs
```

### Logging

A clone logs to the console and ships nothing anywhere. The server URL is the only switch: set `EvilBrains__EvilCase__Logging__Seq__ServerUrl` in your `.env` to ship to a Seq server of your own, and `EvilBrains__EvilCase__Logging__Seq__ApiKey` where that server requires one.

## Frontend–API communication

Typed clients in `EvilCase.Api.Client` are generated at build time from the API controller sources by a source generator, so client and server cannot drift. Controllers are the single source of truth, DTOs are shared through `EvilCase.Api.Contract`, and controller conventions are enforced by analyzers at error severity. The app is served by the API host, so calls are same-origin and no CORS is involved.
