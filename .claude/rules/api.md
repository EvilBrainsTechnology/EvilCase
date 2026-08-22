---
paths:
  - "src/Api/**"
  - "src/EvilCase.Host/**"
---

# API and hosting

`EvilCase.Host` is the composition root. One process serves everything: `/api/**` is the API,
every other path returns the frontend's `index.html`.

- Same-origin only. No CORS anywhere.
- Security headers including the CSP come from `SecurityHeadersMiddleware`. Changing an inline
  script in `index.html` changes the CSP, which names its hash.
- Rate limiting covers the anonymous auth endpoints and the client log upload, nothing else.
  `UseRateLimiter` stays after `UseForwardedHeaders` and before `UseAuthentication`.
- A health response never carries a description or exception detail, and `/health/live` never
  runs a dependency check.
- `EvilBrains:EvilCase:Hosting` adapts the pipeline to whatever sits in front of it; semantics in
  `deploy/README.md`.

## API client

Controllers are the single source of truth and DTOs live in `EvilCase.Api.Contract`; the client
is generated from the controller sources. The `EB1xxx` diagnostics are the spec for both: read
the diagnostic, never work around it. A controller has no constructor; an action takes each
dependency as a `[FromServices]` parameter.

## Secrets and logging

Secrets come from environment variables in every environment; Development additionally loads
`src/EvilCase.Host/.env` through DotNetEnv. Three constraints must not change: it runs before
`CreateBuilder`, `NoClobber()`, `TraversePath()`. `ASPNETCORE_ENVIRONMENT` is read before the
builder exists, so `dotnet run --environment` has no effect.

Read the two READMEs under `src/Utils/EvilBrains.Logging.*` before changing the logging pipeline.
