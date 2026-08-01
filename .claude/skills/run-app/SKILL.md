---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`.

## Prerequisites

- `dotnet tool restore` — `r` is a local tool; without it every command below fails with `Cannot find command r`.
- `EvilCase.Host/.env` with the connection string and JWT key (copy from `.env.example`) — the host fails without them.
- A reachable PostgreSQL. The host migrates the database on startup and does not retry, so an unreachable server stops it before it serves anything. To start without one, set `EvilBrains__EvilCase__Database__MigrateOnStartup=false` — `/health/ready` then answers `503`.
- Trusted dev certificate: `dotnet dev-certs https --trust`.

## Start

One server serves everything: `dotnet r run` → `https://localhost:5000` (Scalar UI at `/scalar`, Development only).

In Claude Code, prefer the preview server defined in `.claude/launch.json` (name `evilcase`). It runs the `claude` launch profile on `https://localhost:5100`, so it never collides with an instance started from the IDE on 5000.

The URLs below use **5100**, the preview port. Replace it with 5000 when verifying an instance started by `dotnet r run`.

## Verify

- Health: `curl.exe -sk https://localhost:5100/health/ready` → `{"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}`. `503` means the database is unreachable; `/health/live` answers even then.
- API round-trip: `curl.exe -sk -X POST https://localhost:5100/api/echo/post -H "Content-Type: application/json" -d '{"message":"ping"}'` → `{"message":"Echo: ping"}`
- Unknown API path: `curl.exe -sk -o /dev/null -w "%{http_code}" https://localhost:5100/api/nope` → `404`, never the app's HTML.
- Frontend: open `https://localhost:5100/echo`, type text, click Send → page shows `Echo: <text>`. First WebAssembly load takes a few seconds.

## Stop

Ctrl+C in the terminal, or stop the preview server.
