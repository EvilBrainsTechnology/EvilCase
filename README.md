# EvilCase

A case-file system for administrative and legal proceedings: a case accumulates acts, contacts, file marks, files and comments, and hangs under an optional parent case. Built so far — the domain model in PostgreSQL, authentication, and a Blazor WebAssembly frontend for cases, acts, contacts, files, comments and a dashboard over them.

> **Proprietary — all rights reserved.** This repository is public to read, not to use. No right to run, copy, modify or distribute the software is granted; see [LICENSE.txt](LICENSE.txt) and ask before you use anything.

## Stack

.NET 10, PostgreSQL, Serilog. The frontend is Blazor WebAssembly on the application's own `Ec` components and stylesheet; the browser libraries and fonts are installed by npm at build time and served by the app — no CDN.

One process serves everything: `/api/*` goes to the API, every other path returns the WebAssembly app.

## Repository layout

All code lives in `src/` (solution `EvilCase.slnx`): `EvilCase.Host` is the only runnable project, `Api/` holds the API, its shared contracts and the generated client, `App/` the frontend, `Data/` the EF Core model and migrations, `Common/` and `Utils/` the shared libraries.

Full project map: [CLAUDE.md](CLAUDE.md); conventions: `.claude/rules/`. Deployment: [deploy/README.md](deploy/README.md).

## Local development

### Prerequisites

- .NET SDK per `src/global.json`
- Node.js 22 or newer with npm: the .NET build runs `npm ci` and generates `wwwroot/lib` and `wwwroot/fonts` from `src/App/EvilCase.App/package.json`. It needs network access to `registry.npmjs.org` and `cdn.sheetjs.com`.
- Docker, for `dotnet r test`. The tests start their own PostgreSQL through Testcontainers, as a container named evilcase-test-db-<slug>, and take it down when the run ends.
- A reachable PostgreSQL, for running the app. The host migrates the database on startup and does not retry, so an unreachable server stops it. Set `EvilBrains__EvilCase__Database__MigrateOnStartup=false` to start without one. A throwaway one, matching the connection string in `.env.example`:

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

Registration is closed, so signing in needs the administrator seeded from `EvilBrains__EvilCase__Auth__Seed__Email` and `EvilBrains__EvilCase__Auth__Seed__Password` — set both before the first start against an empty database. The seed also creates the account and the tenant.

`EvilBrains__EvilCase__Database__SeedSampleData=true` fills a tenant that holds no case with the pseudonymised sample case from `test-data/case-01-speeding.md`; Development has it on.

### Everything in Docker

`dotnet r run-docker` builds the image from this repository and runs it with its own PostgreSQL at `http://localhost:8080`, seeding `admin@evilcase.local` / `DevPassword123!`. Of the prerequisites above only the SDK is needed, plus Docker (the image installs the npm packages in its own Node stage) — no `.env`, no certificate, no database of one's own. Details in [deploy/README.md](deploy/README.md).

### Tests

`dotnet r test` needs Docker, not the PostgreSQL above. The tests covering the `Created` and `Updated` stamps write real rows and read back what the database put there; without a container they fail and name what is missing, they never skip. Every run builds its own database from the migrations and takes it down again.

```
dotnet r test                # tests only
dotnet r ci                  # ai-check + format-check + build + test
```

### Logging

A clone logs to the console and ships nothing anywhere. The server URL is the only switch: set `EvilBrains__EvilCase__Logging__Seq__ServerUrl` in your `.env` to ship to a Seq server of your own, and `EvilBrains__EvilCase__Logging__Seq__ApiKey` where that server requires one.

## Frontend–API communication

Typed clients in `EvilCase.Api.Client` are generated at build time from the API controller sources by a source generator, so client and server cannot drift. Controllers are the single source of truth, DTOs are shared through `EvilCase.Api.Contract`, and controller conventions are enforced by analyzers at error severity. The app is served by the API host, so calls are same-origin and no CORS is involved.

## Integrations over REST

An integration authenticates with a personal API token. Create one in Nastavení → API tokeny (`/settings/api-tokens`) with a name, a scope (Jen čtení or Čtení i zápis) and an expiry of 30, 90 or 365 days; the token is shown once. Send it on every request:

```
curl -H "Authorization: Bearer evc_…" "https://localhost:5000/api/cases?take=20"
```

The token acts as its user, tenant included. A read-only token gets 403 on every write, and an unknown, revoked or expired token gets 401. `/api/auth/**` and token management accept the browser session only. `/openapi/v1.json` describes every endpoint and the Bearer scheme, and is anonymous in every environment.

## AI agents over MCP

`/mcp` is an MCP server (Streamable HTTP) for AI agents, with a tool for every REST resource except token management and the file archive. It accepts an API token only; the browser session and an access token get 401. Connect Claude Code:

```
claude mcp add --transport http evilcase https://<host>/mcp --header "Authorization: Bearer <token>"
```

A read-only token lists and calls only the read tools. File bytes never travel through a tool: `create_file_upload_link` and `create_file_download_link` return a one-time URL, valid 10 minutes, with a curl example; the upload is a PUT of the raw bytes. `read_file_text` returns the text of a text file, PDF or DOCX in pages of 50,000 characters.
