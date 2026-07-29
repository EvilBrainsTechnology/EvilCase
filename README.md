# EvilCase

Case-file management system. Proof-of-concept state: ASP.NET Core API + Blazor WebAssembly frontend with a single echo round-trip.

## Repository structure

All code lives in `src/` (solution `EvilCase.slnx`):

- `Api/EvilCase.Api` — ASP.NET Core API
- `Api/EvilCase.Api.Client` — typed API client (generated from API controllers)
- `Api/EvilCase.Api.Contract` — shared request/response contracts
- `App/EvilCase.App` — Blazor WebAssembly frontend
- `Common/` — auth (JWT), secrets (Infisical)
- `Data/` — EF Core model + migrations (PostgreSQL)
- `Tests/` — application tests
- `Utils/` — shared `EvilBrains.*` libraries and analyzers

AI agent instructions: [AGENTS.md](AGENTS.md).

## Local Development

### Prerequisites

- .NET SDK per `src/global.json`
- Trusted dev certificate: `dotnet dev-certs https --trust`

### Secrets Access

Secrets are saved in [Infisical](https://infisical.com/)  here: https://infisical.vdolek.cz/.

- Obtain your own client secret in Infisical [here](https://infisical.vdolek.cz/organization/identities/1fee778e-ad7f-450a-b927-0f9e49c3d022).
- Add this secret as `EvilBrains:EvilCase:Infisical:ClientSecret` configuration to your local secrets.
  - You can use `dotnet r add-secret` command.

### Build and run

From `src/`:

```
dotnet r build
dotnet r run-api   # API at https://localhost:5000 (Scalar UI at /scalar)
dotnet r run-app   # frontend at https://localhost:5001
```

## Frontend–API communication

The frontend calls the API through typed clients from `EvilCase.Api.Client`, generated at build time from the API controller sources by the `EvilBrains.ApiClient.Generator` source generator (the client project has no dependency on the API project). Controllers are the single source of truth and DTOs are shared via `EvilCase.Api.Contract`, so client and server cannot drift; controller conventions are enforced by analyzers. Development CORS on the API allows the frontend origin `https://localhost:5001`.
