---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`, except the `docker compose` ones, which take a path from the
repository root.

## Prerequisites

- `dotnet tool restore` — `r` is a local tool.
- `EvilCase.Host/.env` copied from `.env.example`, with the connection string, a JWT key of at
  least 32 characters, and `EvilBrains__EvilCase__Auth__Seed__Email` and `__Password` — the
  seeded administrator is the only way into an empty database.
- A reachable PostgreSQL — the host migrates on startup and does not retry. A throwaway one,
  from the repository root, idempotent, matching the connection string in `.env.example`:

  ```bash
  docker compose -f deploy/docker-compose.dev.yml up -d --wait
  ```

  Without a database, `EvilBrains__EvilCase__Database__MigrateOnStartup=false` starts the host,
  but `/health/ready` answers `503` and sign-in fails, so nothing behind the login page can be
  verified.
- Trusted dev certificate: `dotnet dev-certs https --trust`.

### Claude Code on the web

`.claude/hooks/session-start.sh` does all of the above at session start, only where
`CLAUDE_CODE_REMOTE` is `true`: it copies the SDK out of `mcr.microsoft.com/dotnet/sdk:10.0`
(the SDK installers are egress-blocked), installs `pwsh` from NuGet (`src/global.json` names it
as `scriptShell`), starts the container's own PostgreSQL, and writes a `.env` with throwaway
credentials (`admin@evilcase.local` / `DevPassword123!`). The hook restates the SDK version, the
connection string, the seed and JWT keys and the `src/` layout — a change to any of those is a
change to the hook, in the same commit.

The hook writes `.env` into the main checkout only, so a worktree has none and the host will
not start there: copy the file into the worktree, or run the app from the main checkout.

## Start

`dotnet r run` verifies a change; take `dotnet r run-docker` only to verify the image itself. It
runs the image as `Development`, so it proves the image, never the deployed configuration, and
it costs an image build per change.

One machine runs one application, and both ways hardcode their address, so parallel agents take
turns rather than a port each. `dotnet r run` at least fails on the taken port; the Docker stack
is named `evilcase-local` whatever the checkout, so a second one takes the first's containers
over and serves its own build on the same `http://localhost:8080` — silently, and the screenshot
is then of another worktree's code. Verify against the address of what this session started, and
stop it before handing the machine on.

`dotnet r run` → `https://localhost:5000` (Scalar UI at `/scalar`, Development only). In Claude
Code, start the preview server `evilcase` from `.claude/launch.json` instead of a shell; it
serves the same address, and only one instance can hold the port — stop an IDE instance first.
Keep the port off the browsers' unsafe-port list (6000, 6665–6669, 6697, …).

`dotnet r run-docker` → `http://localhost:8080`, with its own PostgreSQL and the administrator
`admin@evilcase.local` / `DevPassword123!`; of the prerequisites above only `dotnet tool
restore` applies, and Docker. In Claude Code, open the browser pane at that URL — the preview
server of `.claude/launch.json` serves the other one. `deploy/README.md` has the stack.

## Verify

Against the Docker stack, read `http://localhost:8080` for every `https://localhost:5000` below.

- `curl -sk https://localhost:5000/health/ready` → `Healthy` with the `database` check; `503`
  means the database is unreachable, `/health/live` answers even then.
- Sign in: `POST /api/auth/login` with `{"email":…,"password":…}` (the seed values) →
  `accessToken`; `401` is bad credentials, `423` a lockout (5 failures, 15 minutes).
- API round-trip: `POST /api/echo/post` with the bearer → `Echo: …`; without the header `401`,
  which is the fallback policy working.
- `GET /api/nope` → `404`, never the app's HTML.
- Frontend: `https://localhost:5000` redirects to `/login`; sign in, open `/echo`, send a text.
  The first WebAssembly load takes a few seconds.

## Stop

Ctrl+C, or stop the preview server. The database keeps running;
`docker compose -f deploy/docker-compose.dev.yml down` removes it along with its data. The
Docker stack stops the same way and is removed with
`docker compose -f deploy/docker-compose.local.yml down`.
