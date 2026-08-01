# EvilCase

Case-file management system. Proof-of-concept state: ASP.NET Core API + Blazor WebAssembly frontend with a single echo round-trip.

> **Proprietary — all rights reserved.** This repository is public to read, not to use. No right to run, copy, modify or distribute the software is granted; see [LICENSE.txt](LICENSE.txt) and ask before you use anything.

## Repository structure

All code lives in `src/` (solution `EvilCase.slnx`):

- `EvilCase.Host` — the single web host: serves the API and the frontend
- `Api/EvilCase.Api` — ASP.NET Core API (library, not runnable on its own)
- `Api/EvilCase.Api.Client` — typed API client (generated from API controllers)
- `Api/EvilCase.Api.Contract` — shared request/response contracts
- `App/EvilCase.App` — Blazor WebAssembly frontend
- `Common/` — auth (JWT)
- `Data/` — EF Core model + migrations (PostgreSQL)
- `Tests/` — application tests
- `Utils/` — shared `EvilBrains.*` libraries and analyzers

AI agent instructions: [AGENTS.md](AGENTS.md).

## Local Development

### Prerequisites

- .NET SDK per `src/global.json`
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
dotnet r build
dotnet r run     # everything at https://localhost:5000 (Scalar UI at /scalar)
```

One process serves both: `/api/*` goes to the API, everything else returns the WebAssembly app.

## Frontend–API communication

The frontend calls the API through typed clients from `EvilCase.Api.Client`, generated at build time from the API controller sources by the `EvilBrains.ApiClient.Generator` source generator (the client project has no dependency on the API project). Controllers are the single source of truth and DTOs are shared via `EvilCase.Api.Contract`, so client and server cannot drift; controller conventions are enforced by analyzers. The app is served by the API host, so calls are same-origin and no CORS is involved.
