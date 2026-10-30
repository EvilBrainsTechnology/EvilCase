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
- Rate limiting covers `/api/auth/**` and the client log upload, nothing else. `UseRateLimiter`
  stays after `UseForwardedHeaders` and before `UseAuthentication`.
- Health responses carry no description or exception detail; `/health/live` checks no dependency.
- `EvilBrains:EvilCase:Hosting` adapts the pipeline to what fronts it; see `deploy/README.md`.

## API client

Controllers are the single source of truth and DTOs live in `EvilCase.Api.Contract`; the client
is generated from the controller sources. The `EB0xxx` and `EB1xxx` diagnostics are the spec for
both: read the diagnostic, never work around it. A controller has no constructor; an action takes
each dependency as a `[FromServices]` parameter. Every action parameter carries an explicit
binding attribute, in order: `[FromServices]`, `[FromRoute]`, `[FromQuery]` and `[FromForm]`,
`[FromBody]`, `CancellationToken`.

## Secrets and logging

Secrets come from environment variables in every environment; Development additionally loads
`src/EvilCase.Host/.env` through DotNetEnv. Three constraints must not change: it runs before
`CreateBuilder`, `NoClobber()`, `TraversePath()`. Set `ASPNETCORE_ENVIRONMENT` in the environment;
`dotnet run --environment` has no effect. Read the two READMEs under
`src/Utils/EvilBrains.Logging.*` before changing the logging pipeline.
