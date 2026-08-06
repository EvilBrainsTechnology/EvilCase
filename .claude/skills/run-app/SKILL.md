---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

`Start-EvilCase.ps1` next to this file is how a validation runs the app: agents run side by side,
so every run takes a port and a database of its own. `dotnet r run` and the `evilcase` preview
server share the fixed 5000 and belong to whoever took it first.

## Prerequisites

- `dotnet tool restore` from `src/`; `dotnet dev-certs https`.
- PostgreSQL on `localhost:5432` as `postgres`/`postgres`; `-PostgresHost` and the parameters
  beside it reach another server. `README.md` has a throwaway one.
- `src/EvilCase.Host/.env` with a JWT key of at least 32 characters and
  `EvilBrains__EvilCase__Auth__Seed__Email` and `__Password` — the seeded administrator is the
  only way into an empty database. The script supplies neither.

In Claude Code on the web `.claude/hooks/session-start.sh` does all of it at session start and
restates the connection string, the seed and the JWT keys; a change to any of them changes the
hook in the same commit. The `.env` lands in the main checkout only, and a worktree copies it:

```bash
cp "$(git rev-parse --path-format=absolute --git-common-dir)/../src/EvilCase.Host/.env" src/EvilCase.Host/.env
```

## Run

From the repository root. The start builds, waits for `/health/ready` and prints the URL, and
nothing but the URL, on stdout; the script's header documents its parameters.

```bash
pwsh .claude/skills/run-app/Start-EvilCase.ps1                    # → https://localhost:41449
pwsh .claude/skills/run-app/Start-EvilCase.ps1 -Stop -Port 41449
```

## Verify

`$url` is what the start printed. The run is Development, so Scalar is at `$url/scalar` and
`EVILCASE_URL` points `screenshots.mjs` at it.

- `curl -sk $url/health/ready` → `Healthy` with the `database` check; `503` is an unreachable
  database, and `/health/live` answers even then.
- `POST /api/auth/login` with the seed values from `.env` → `accessToken`; `401` is bad
  credentials, `423` a lockout (5 failures, 15 minutes).
- `POST /api/echo/post` with the bearer and `{"message":…}` → `Echo: …`, without it `401`.
- `GET /api/nope` → `404`, never the app's HTML.
- `$url` redirects to `/login`; sign in, open `/echo`, send a text. The first WebAssembly load
  takes a few seconds.
