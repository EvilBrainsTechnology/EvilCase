---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`, except the database one, which takes a path from the repository
root.

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
as `scriptShell`), starts the container's own PostgreSQL, writes a `.env` with throwaway
credentials (`admin@evilcase.local` / `DevPassword123!`), and pins the repository's commit
identity to `claude[bot]` — worktrees share it. The hook restates the SDK version, the
connection string, the seed and JWT keys and the `src/` layout — a change to any of those is a
change to the hook, in the same commit.

The hook writes `.env` into the main checkout only, so a worktree has none and the host will
not start there until the file is copied into it.

## Start

`dotnet r run` → `https://localhost:5000` (Scalar UI at `/scalar`, Development only). In Claude
Code, start the preview server `evilcase` from `.claude/launch.json` instead of a shell; it
serves the same address, and only one instance can hold the port — stop an IDE instance first.
A port picked by hand stays off the browsers' unsafe-port list (6000, 6665–6669, 6697, …).

Subagents run side by side, so 5000 and the `evilcase` database belong to whoever took them
first. `Start-EvilCase.ps1` next to this file gives a run its own of both, prints the URL and
documents itself in its header; it supplies no JWT key or seed, so a worktree still needs `.env`.
Point the screenshot script at the printed URL with `EVILCASE_URL`.

```
pwsh .claude/skills/run-app/Start-EvilCase.ps1                    # → https://localhost:41449
pwsh .claude/skills/run-app/Start-EvilCase.ps1 -Stop -Port 41449
```

## Verify

Against the URL the run printed — `$url` below, `https://localhost:5000` for `dotnet r run`.

- `curl -sk $url/health/ready` → `Healthy` with the `database` check; `503` means the database
  is unreachable, `/health/live` answers even then.
- Sign in: `POST /api/auth/login` with `{"email":…,"password":…}` (the seed values) →
  `accessToken`; `401` is bad credentials, `423` a lockout (5 failures, 15 minutes).
- API round-trip: `POST /api/echo/post` with the bearer → `Echo: …`; without the header `401`,
  which is the fallback policy working.
- `GET /api/nope` → `404`, never the app's HTML.
- Frontend: `$url` redirects to `/login`; sign in, open `/echo`, send a text. The first
  WebAssembly load takes a few seconds.

## Stop

`Start-EvilCase.ps1 -Stop -Port <port>`, on the port the start printed — the header has the rest.
`dotnet r run` stops with Ctrl+C or by stopping the preview server, and leaves the `evilcase`
database running: `docker compose -f deploy/docker-compose.dev.yml down` removes it along with
its data.
