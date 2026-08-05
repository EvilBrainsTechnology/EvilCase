---
paths:
  - "src/Api/**"
  - "src/EvilCase.Host/**"
---

# API and hosting

`EvilCase.Host` is the composition root: `Program.cs`, the middleware pipeline, all
configuration. One process serves everything: `/api/**` is the API, every other path returns the
frontend's `index.html`. An unmatched `/api` path answers a problem-details `404`, never HTML.

- Controller `[Route]` templates carry the `api/` prefix themselves; analyzers enforce it.
- Same-origin only, no CORS anywhere; the frontend takes its base address from
  `builder.HostEnvironment.BaseAddress`.
- The two `EvilBrains:EvilCase:Hosting` keys, `BehindReverseProxy` and `HttpsRedirection`, adapt
  the pipeline to what sits in front of it; semantics in `deploy/README.md`.
- Security headers including the CSP come from `SecurityHeadersMiddleware`. The CSP names the
  hash of every inline script in `index.html`, so changing one changes the policy. `/scalar` is
  excluded.
- The anonymous auth endpoints and the client log upload are rate limited per caller address;
  nothing else is, health probes above all. `UseRateLimiter` stays after `UseForwardedHeaders`
  and before `UseAuthentication`.
- Health endpoints are mapped anonymously outside the controllers; `/health/live` never runs a
  dependency check; health responses never carry descriptions or exception detail.
- Endpoints are default-deny; `[AllowAnonymous]` is an owner decision — `.claude/rules/auth.md`.

## API client

Controllers are the single source of truth; DTOs live in `EvilCase.Api.Contract`.
`EvilCase.Api.Client` never references `EvilCase.Api`: it includes the controller sources as
`AdditionalFiles` and the source generator emits a client for every `[GenerateApiClient]`
controller. Generated routes are relative; `AddEvilCaseApiClient` normalises the base address.

`EB1001`–`EB1016` are the controller and client spec: read the diagnostic, never work around it.
`[FromForm]` and `IFormFile` are not supported.

## Secrets and logging

Secrets come from environment variables in every environment; Development additionally loads
`src/EvilCase.Host/.env` through DotNetEnv. Three constraints must not change: it runs before
`CreateBuilder`, `NoClobber()`, `TraversePath()`. `ASPNETCORE_ENVIRONMENT` is read before the
builder exists, so `dotnet run --environment` has no effect.

Read the two READMEs under `src/Utils/EvilBrains.Logging.*` before changing the logging pipeline.
