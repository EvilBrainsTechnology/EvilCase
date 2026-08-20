# Deployment

The application runs as a single container image published to `ghcr.io/evilbrainstechnology/evilcase`.

## Image

Built from the repository root `Dockerfile`; the build context is the root and the solution lives in `src/`. `.dockerignore` excludes everything outside `src/` except `LICENSE.txt`.

- `sdk:10.0` restores and publishes `EvilCase.Host`. The Blazor WebAssembly bundle comes with it, there is no separate frontend step.
- `aspnet:10.0-alpine` runs it as the image's non-root user on port 8080. Entry point `EvilBrains.EvilCase.Host.dll` — `src/Directory.Build.props` renames the assembly.
- `HEALTHCHECK` calls `/health/live` through `curl`.

## Registry tags

`.github/workflows/Docker.yml` calls `CI.yml` as a reusable workflow and only builds if it passes, so nothing is published from a commit that fails lint, build or tests.

| Trigger | Tags |
| --- | --- |
| Push to `master` | `edge`, `master-<sha>` |
| Published release `v1.2.3` | `1.2.3`, `1.2`, `1`, `latest` |
| Published prerelease | the version only, never `latest` |
| Manual run from another branch | the branch name |

The manual-run rule exists so such a run matches at least one tag: an empty tag list pushes nothing and leaves the provenance attestation without a digest to sign.

The release tag also becomes the assembly version, through the `VERSION` build argument; `SOURCE_REVISION` carries the commit. Anything else builds as `0.0.0`. No MinVer or GitVersion — `.git` is not in the build context.

The provenance statement is stored on GitHub, not in the registry. Verify with `gh attestation verify oci://ghcr.io/evilbrainstechnology/evilcase --repo <owner>/<repo>`.

## Hosting keys

Two keys under `EvilBrains:EvilCase:Hosting` adapt the pipeline to what sits in front of it:

- `BehindReverseProxy` (default `false`) calls `UseForwardedHeaders` first, with `KnownIPNetworks` and `KnownProxies` cleared and `ForwardLimit = 1`. The single trusted hop is the whole defence: a deployment that turns it on must not be reachable except through that proxy.
- `HttpsRedirection` (default `true`) turns `UseHttpsRedirection` off where something in front already redirects. `/health/*` is excluded from redirection either way.

## Compose stack

`docker-compose.yml` runs the application; `.env` next to it (gitignored, `.env.example` documents the keys) holds the variables.

The database is not part of the stack — `EVILCASE_CONNECTION_STRING` points at an existing PostgreSQL. The schema is migrated on startup unless `EvilBrains__EvilCase__Database__MigrateOnStartup=false` is added to the service environment; where more than one instance starts at once, roll it out separately from the idempotent `database.sql` artifact CI publishes. The role in the connection string must be able to `CREATE EXTENSION`: the `Init` migration installs `unaccent` and `pg_trgm`.

The service is published over plain HTTP for a reverse proxy that terminates TLS, so it sets `BehindReverseProxy=true` and `HttpsRedirection=false`; the port is published on `127.0.0.1` only (`EVILCASE_PORT` picks the host port), keeping the service unreachable except through the proxy.

Seq is driven by `EVILCASE_SEQ_URL` alone — an empty one logs to the console only.

```
cp .env.example .env   # then fill in the connection string and the JWT key
docker compose up -d
```

## Local application stack

`docker-compose.local.yml` (project name `evilcase-local`) is the deployed stack's development
twin: it builds the image from the repository instead of pulling it, and runs its own PostgreSQL
next to it. `dotnet r run-docker` from `src/` is the whole command — no `.env`, no dev
certificate, no database of one's own.

It is there for a person who wants the application running without setting anything up. Verifying
a change is the run-app skill, which says nothing of this stack on purpose: one address, one
database and one image build per change are not what a validation needs.

```
docker compose -f deploy/docker-compose.local.yml up --build
```

The application answers on `http://localhost:8080` (`EVILCASE_PORT` picks another host port, and
`deploy/.env` of the stack above is read here too) and seeds `admin@evilcase.local` /
`DevPassword123!`. Plain HTTP with nothing in front, so `HttpsRedirection` is off and
`BehindReverseProxy` stays at its default.

The image is the deployed one, the configuration is not: it runs as `Development`, which maps
Scalar at `/scalar` and lets EF Core log sensitive data. `EVILCASE_PORT` is the only variable it
takes from that `.env` — `EVILCASE_SEQ_URL` does not reach it, so it logs to the console only.

The database publishes no port: the application reaches it over the compose network, and
publishing would collide with the development stack below. Like it, it is throwaway — constant
credentials, and its data directory is a `tmpfs`, so it lives in RAM and dies with the container.

## Local development database

`docker-compose.dev.yml` is a separate stack (project name `evilcase-dev`) that runs PostgreSQL only, on `127.0.0.1:5432` with the credentials `.env.example` of the host already points at. It is for development, never for a deployment: the password is a constant and the data directory is a `tmpfs`, so the data lives in RAM, `down` wipes it and the next start migrates and seeds an empty database again.

```
docker compose -f deploy/docker-compose.dev.yml up -d --wait
```

`--wait` returns once the health check passes, so the host cannot start ahead of it.
