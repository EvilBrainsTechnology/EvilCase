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

The database is not part of the stack — `EVILCASE_CONNECTION_STRING` points at an existing PostgreSQL. The schema is migrated on startup unless `EvilBrains__EvilCase__Database__MigrateOnStartup=false` is added to the service environment; where more than one instance starts at once, roll it out separately from the idempotent `database.sql` artifact CI publishes.

The service is published over plain HTTP for a reverse proxy that terminates TLS, so it sets `BehindReverseProxy=true` and `HttpsRedirection=false`; the port is published on `127.0.0.1` only (`EVILCASE_PORT` picks the host port), keeping the service unreachable except through the proxy.

Seq is driven by `EVILCASE_SEQ_URL` alone — an empty one logs to the console only.

```
cp .env.example .env   # then fill in the connection string and the JWT key
docker compose up -d
```

## Local application stack

`docker-compose.local.yml` is the deployed stack's development twin: it builds the image from the
repository instead of pulling it, and runs its own PostgreSQL next to it. It is how the
application is run and verified locally, by a person and by an agent alike.

```
dotnet r run-docker    # from src/; dotnet r stop-docker removes it again
```

`Local-Stack.ps1` behind those two is what makes it safe to run more than once: the compose
project name is a digest of the checkout's path and the host port is whatever Docker had free, so
worktrees never share containers, an image or an address. The script prints the address; nothing
else knows it. `EVILCASE_PORT` pins the port where a stable one is wanted — `deploy/.env` of the
stack above is read here too, so a port set for the deployment reaches this stack as well.

Every start rebuilds the image, about a minute, and what runs is therefore the working tree. The
seeded administrator is `admin@evilcase.local` / `DevPassword123!`. Plain HTTP with nothing in
front, so `HttpsRedirection` is off and `BehindReverseProxy` stays at its default.

The image is the deployed one, the configuration is not: it runs as `Development`, which maps
Scalar at `/scalar` and lets EF Core log sensitive data. The Seq URL of
`appsettings.Development.json` is cleared, so a local run ships no logs to a real server.

The database publishes no port at all — the application reaches it over the compose network — and
its data directory is a `tmpfs`, so it lives in RAM and dies with the container.

## Local development database

Running the host from the SDK is the shorter loop and the only way to attach a debugger; it needs
a PostgreSQL of its own. One container, throwaway, matching the connection string in
`src/EvilCase.Host/.env.example`:

```
docker run -d --name evilcase-db -p 127.0.0.1:5432:5432 --tmpfs /var/lib/postgresql -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=evilcase postgres:18-alpine
```

Never for a deployment: the password is a constant and the data is in RAM, so `docker rm -f
evilcase-db` wipes it and the next start migrates and seeds an empty database again.
