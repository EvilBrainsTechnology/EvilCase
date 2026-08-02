---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`, except the database one, which takes a path from the repository root.

## Prerequisites

- `dotnet tool restore` — `r` is a local tool; without it every command below fails with `Cannot find command r`.
- `EvilCase.Host/.env` (copy from `.env.example`). The host fails to start without the connection string and a JWT key of at least 32 characters. Fill in `EvilBrains__EvilCase__Auth__Seed__Email` and `EvilBrains__EvilCase__Auth__Seed__Password` too — registration is closed, so an empty database has no way in otherwise.
- A reachable PostgreSQL. The host migrates the database on startup and does not retry, so an unreachable server stops it before it serves anything. Start a throwaway one, from the repository root:

  ```bash
  docker compose -f deploy/docker-compose.dev.yml up -d --wait
  ```

  It listens on `127.0.0.1:5432` with `postgres`/`postgres`/`evilcase`, which is exactly the connection string in `.env.example`. `--wait` returns only once the health check passes, so the host cannot start ahead of it. The command is idempotent: it starts a stopped container and returns immediately for a running one.

  Without a database, `EvilBrains__EvilCase__Database__MigrateOnStartup=false` starts the host anyway, but `/health/ready` answers `503` and anything touching data fails. Sign-in is one of those things, so nothing behind the login page can be verified that way.
- Trusted dev certificate: `dotnet dev-certs https --trust`.

### Claude Code on the web

`.claude/hooks/session-start.sh` does all of the above and runs automatically at session start, so a
web session needs no manual setup — `dotnet r build`, `dotnet r test` and `dotnet r run` work straight
away. It only runs where `CLAUDE_CODE_REMOTE` is `true`; a local machine is left alone.

Two things differ from the list above, because the container has no .NET SDK and the egress policy
blocks `builds.dotnet.microsoft.com`:

- The SDK is copied out of `mcr.microsoft.com/dotnet/sdk:10.0` onto the host filesystem, which is the
  only reachable source. Docker is used for that one copy and for nothing afterwards. `pwsh` is not in
  that layout and comes from NuGet instead — `global.json` sets it as `scriptShell`, so every
  `dotnet r` script needs it.
- PostgreSQL is the container's own 16, started with `service postgresql start`, rather than the 18 in
  `deploy/docker-compose.dev.yml`. Credentials, port and database name are the same, so the connection
  string in `.env.example` is unchanged.

The generated `.env` seeds `admin@evilcase.local` / `DevPassword123!`. Those credentials are throwaway
and local to the container.

`.claude/launch.json` runs the app on the host, so it works once the hook has run.

## Start

One server serves everything: `dotnet r run` → `https://localhost:5000` (Scalar UI at `/scalar`, Development only).

In Claude Code, start it through the preview server defined in `.claude/launch.json` (name `evilcase`) rather than from a shell. It runs the `claude` launch profile, which differs from `https` only in launching no browser — same port, so an instance already running from the IDE has to be stopped first.

## Verify

The application is closed by default: every endpoint except health, sign-in and the client log upload needs a bearer token, and every page except `/login` needs a signed-in user.

- Health: `curl.exe -sk https://localhost:5000/health/ready` → `{"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}`. `503` means the database is unreachable; `/health/live` answers even then.
- Sign in with the seeded administrator and keep the access token:

  ```bash
  curl.exe -sk -X POST https://localhost:5000/api/auth/login -H "Content-Type: application/json" -d '{"email":"<seed email>","password":"<seed password>"}'
  ```

  → `{"accessToken":"...","expiresAt":"...","email":"...","role":"Administrator"}`. `401` is bad credentials, `423` a lockout (5 failures, 15 minutes).
- API round-trip: `curl.exe -sk -X POST https://localhost:5000/api/echo/post -H "Content-Type: application/json" -H "Authorization: Bearer <accessToken>" -d '{"message":"ping"}'` → `{"message":"Echo: ping"}`. Without the header it is `401`, which is the fallback policy working, not a failure.
- Unknown API path: `curl.exe -sk -o /dev/null -w "%{http_code}" https://localhost:5000/api/nope` → `404`, never the app's HTML.
- Frontend: open `https://localhost:5000`, which redirects to `/login`; sign in with the seeded administrator, then open `/echo`, type text and click Send → page shows `Echo: <text>`. First WebAssembly load takes a few seconds.

## Stop

Ctrl+C in the terminal, or stop the preview server. The database keeps running; `docker compose -f deploy/docker-compose.dev.yml down` removes it along with its data.
