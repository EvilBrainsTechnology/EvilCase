---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`, except the database one, which takes a path from the repository root.

## Prerequisites

- `dotnet tool restore` — `r` is a local tool; without it every command below fails with `Cannot find command r`.
- `EvilCase.Host/.env` (copy from `.env.example`). The host fails to start without the connection string and a JWT key of at least 32 characters. Fill in `Auth__Seed__Email` and `Auth__Seed__Password` too — registration is closed, so an empty database has no way in otherwise.
- A reachable PostgreSQL. The host migrates the database on startup and does not retry, so an unreachable server stops it before it serves anything. Start a throwaway one, from the repository root:

  ```bash
  docker compose -f deploy/docker-compose.dev.yml up -d --wait
  ```

  It listens on `127.0.0.1:5432` with `postgres`/`postgres`/`evilcase`, which is exactly the connection string in `.env.example`. `--wait` returns only once the health check passes, so the host cannot start ahead of it. The command is idempotent: it starts a stopped container and returns immediately for a running one.

  Without a database, `EvilBrains__EvilCase__Database__MigrateOnStartup=false` starts the host anyway, but `/health/ready` answers `503` and anything touching data fails. Sign-in is one of those things, so nothing behind the login page can be verified that way.
- Trusted dev certificate: `dotnet dev-certs https --trust`.

## Start

One server serves everything: `dotnet r run` → `https://localhost:5000` (Scalar UI at `/scalar`, Development only).

In Claude Code, prefer the preview server defined in `.claude/launch.json` (name `evilcase`). It runs the `claude` launch profile on `https://localhost:5100`, so it never collides with an instance started from the IDE on 5000.

The URLs below use **5100**, the preview port. Replace it with 5000 when verifying an instance started by `dotnet r run`.

## Verify

The application is closed by default: every endpoint except health, sign-in and the client log upload needs a bearer token, and every page except `/login` needs a signed-in user.

- Health: `curl.exe -sk https://localhost:5100/health/ready` → `{"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}`. `503` means the database is unreachable; `/health/live` answers even then.
- Sign in with the seeded administrator and keep the access token:

  ```bash
  curl.exe -sk -X POST https://localhost:5100/api/auth/login -H "Content-Type: application/json" -d '{"email":"<seed email>","password":"<seed password>"}'
  ```

  → `{"accessToken":"...","expiresAt":"...","email":"...","role":"Administrator"}`. `401` is bad credentials, `423` a lockout (5 failures, 15 minutes).
- API round-trip: `curl.exe -sk -X POST https://localhost:5100/api/echo/post -H "Content-Type: application/json" -H "Authorization: Bearer <accessToken>" -d '{"message":"ping"}'` → `{"message":"Echo: ping"}`. Without the header it is `401`, which is the fallback policy working, not a failure.
- Unknown API path: `curl.exe -sk -o /dev/null -w "%{http_code}" https://localhost:5100/api/nope` → `404`, never the app's HTML.
- Frontend: open `https://localhost:5100`, which redirects to `/login`; sign in with the seeded administrator, then open `/echo`, type text and click Send → page shows `Echo: <text>`. First WebAssembly load takes a few seconds.

## Stop

Ctrl+C in the terminal, or stop the preview server. The database keeps running; `docker compose -f deploy/docker-compose.dev.yml down` removes it along with its data.
