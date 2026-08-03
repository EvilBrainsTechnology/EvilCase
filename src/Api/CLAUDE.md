# API, hosting and configuration

Covers `src/Api/**` and `src/EvilCase.Host`.

## Hosting

One process serves everything. `EvilCase.Host` is the composition root: it owns `Program.cs`, the middleware pipeline and all configuration (`appsettings*.json`, `.env`, `launchSettings.json`). The dependency runs host → api, never api → app.

- `/api/**` is the API. Controller `[Route]` templates carry the prefix themselves — the client generator reads them from source and would not see a runtime routing convention — and an analyzer enforces it.
- An unmatched `/api` path is a `404` in problem details shape, from a fallback registered in `MapEvilCaseApi`, never the app's HTML: its literal segment gives it precedence over the catch-all `MapFallbackToFile("index.html")`. `Tests/EvilCase.Tests` pins that precedence through the real `Program.cs`.
- Everything else returns `index.html`, so client-side routes survive a reload. `/health/*`, `/scalar` and `/openapi/v1.json` are mapped explicitly and the fallback never reaches them.
- Controllers live in a library, so `AddControllers().AddApplicationPart(...)` registers them explicitly.
- `EvilCase.Api` is `Microsoft.NET.Sdk` + `FrameworkReference Microsoft.AspNetCore.App`, so it has none of the Web SDK's implicit usings — import ASP.NET Core namespaces per file.
- Same-origin: no CORS anywhere. The frontend takes its API base address from `builder.HostEnvironment.BaseAddress`.

Two keys under `EvilBrains:EvilCase:Hosting` adapt the pipeline to what sits in front of it. `BehindReverseProxy` (default `false`) calls `UseForwardedHeaders` first, with `KnownIPNetworks` and `KnownProxies` cleared and `ForwardLimit = 1`; the single hop is then the whole defence, so a deployment that turns it on must not be reachable except through that proxy. `HttpsRedirection` (default `true`) turns `UseHttpsRedirection` off where something in front already redirects. `/health/*` is excluded from redirection either way.

Baseline security headers, the content security policy among them, are written by `SecurityHeadersMiddleware`; `/scalar` is excluded, because the Scalar UI loads its bundle from a CDN. The policy has to name the hash of every inline script of `index.html`, which `SecurityHeadersTests` pins.

The anonymous entry points are rate limited per caller address, each in its own partition: `/api/auth/login` 5 per minute, `/api/auth/refresh` 60, the rest of `/api/auth/*` 10, the client log upload 120. Nothing else is limited, health probes above all. `UseRateLimiter` sits after `UseForwardedHeaders`, so a partition is the caller rather than the proxy, and ahead of `UseAuthentication`, so a rejected caller still spends permits.

`PrincipalOwnerContext` implements `IOwnerContext` here by reading the access token's `sub` claim — the seam is documented in `src/Data/CLAUDE.md`.

## API client pattern

API controllers are the single source of truth; DTOs live in `EvilCase.Api.Contract`. `EvilCase.Api.Client` has no dependency on `EvilCase.Api`: it includes the controller sources as `AdditionalFiles` and the `EvilBrains.ApiClient.Generator` source generator emits clients from them, in memory, never committed. A controller marked `[GenerateApiClient]` produces a public `I{Name}Client` interface, an internal implementation and a DI registration. Consumers register clients via `Bootstrap.AddEvilCaseApiClient`, which takes an optional `Action<IHttpClientBuilder>` so message handlers attach to the generated clients only.

Generated routes are relative (`api/echo/post`, no leading slash) and resolve against the base address, which `AddEvilCaseApiClient` normalises to end in `/`. That is what keeps the app working when it is served from a sub-path.

Controller shape (route templates, HTTP method attributes, kebab-case segments, the `api/` prefix, parameter binding) is `EB1001`–`EB1006`, reported by the analyzers in the API project and re-checked by the generator. Client feasibility (return types, parameter types, type visibility to the client compilation) is `EB1010`–`EB1016`, reported by the generator only. Both are error severity with exact file and line locations: read the diagnostic rather than working around it. `[FromForm]` and `IFormFile` are not supported.

## Health checks

Two anonymous endpoints, mapped with `MapHealthChecks` in `MapEvilCaseApi` rather than through a controller: they carry no client contract, so they stay out of OpenAPI, out of the generated API client and out of the controller conventions. Keep `AllowAnonymous` on both — the authorization fallback policy would otherwise turn every probe into a `401`.

- `GET /health/live` runs no check (`Predicate = _ => false`) and answers `Healthy` as plain text. Never add a dependency check here.
- `GET /health/ready` runs the checks tagged `HealthCheckTags.Ready` and writes names and statuses as JSON: 200 healthy, 503 unhealthy, 503 degraded. `Program.cs` fills it with `AddHealthChecks().AddEvilCaseApiHealthChecks(HealthCheckTags.Ready)`, the top of the per-layer chain `src/Business/CLAUDE.md` describes; the tag is public because the host names it.

`HealthCheckResponseWriter` keeps descriptions, exception text and check data out of the response, because the endpoint is anonymous.

## Secrets and configuration

Every environment reads secrets from environment variables. Development additionally loads `src/EvilCase.Host/.env` (gitignored, `.env.example` documents the keys) into the process environment, so there is one configuration path everywhere — hence the double underscore separator (`A__B` → `A:B`).

`DotNetEnv` does the loading, in `Program.cs`, with three constraints that must not be changed:

- It runs **before** `CreateBuilder`. That call is where `AddEnvironmentVariables` snapshots the process environment; anything set afterwards is invisible to configuration.
- `NoClobber()` — an environment variable that is already set wins over the file, so a `.env` cannot override what a container or CI job passes in.
- `TraversePath()` — the file is searched for upwards from `AppContext.BaseDirectory`, because `dotnet run` keeps the caller's working directory.

The check runs before the builder exists, so `ASPNETCORE_ENVIRONMENT` is read directly rather than through `builder.Environment`. Consequence: `dotnet run --environment X` does not affect it, only the variable does.

`EvilBrains.Secrets.Infisical` holds an Infisical configuration provider. Nothing calls it and `appsettings.json` has no section for it.

## Logging

`EvilBrains.Logging.Contract` holds the wire contract (client log DTOs, header and property names). The server half is documented in `src/Utils/EvilBrains.Logging.AspNetCore/README.md`, the browser half in `src/Utils/EvilBrains.Logging.WebAssembly/README.md`. Read those before changing anything in the pipeline.

- Every event carries `AppSource`, either `Client` or `Server`. The name is reserved: a browser entry cannot claim to be a server one.
- Request logging is an allow-list: `app.UseRequestLogging(loggedPaths: ["/api"], quietPaths: [ClientLogRoute.Path])`. Anything outside `loggedPaths` leaves no completion log unless it fails. Do not turn it into a deny-list — the host also serves the frontend and all its assets.
- The upload route is `ClientLogRoute` in `EvilCase.Api.Contract`, and the controller, the host's quiet path and the browser sink all take it from there. Naming it again anywhere breaks both feedback-loop guards silently.
- Seq is configured from `EvilBrains:EvilCase:Logging:Seq` (`ServerUrl`, `ApiKey`), not from the `Serilog` section, which only holds the console sink. The server URL is the only switch: an environment naming none logs to the console only.
- The `Environment` property is enriched from `builder.Environment.EnvironmentName`, never from an `appsettings.*.json` of its own.
- Seq credentials stay on the server; the browser only ever talks to the API.
