# EvilCase

Case-file management system. Proof-of-concept state: ASP.NET Core API + Blazor WebAssembly frontend with a single echo round-trip.

## Repository structure

All code lives in `src/` (solution `EvilCase.slnx`):

- `Api/EvilCase.Api` — ASP.NET Core API
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

Local secrets live in `src/Api/EvilCase.Api/.env`, which is not committed.

- Copy `src/Api/EvilCase.Api/.env.example` to `src/Api/EvilCase.Api/.env` and fill in the values.
- Keys use the environment variable separator: `A__B` maps to the configuration key `A:B`.
- The file is read in the Development environment only and wins over `appsettings*.json`. Every other environment supplies the same keys as environment variables.

### Build and run

From `src/`:

```
dotnet r build
dotnet r run-api   # API at https://localhost:5000 (Scalar UI at /scalar)
dotnet r run-app   # frontend at https://localhost:5001
```

## Frontend–API communication

The frontend calls the API through typed clients from `EvilCase.Api.Client`, generated at build time from the API controller sources by the `EvilBrains.ApiClient.Generator` source generator (the client project has no dependency on the API project). Controllers are the single source of truth and DTOs are shared via `EvilCase.Api.Contract`, so client and server cannot drift; controller conventions are enforced by analyzers. Development CORS on the API allows the frontend origin `https://localhost:5001`.
