---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`.

## Prerequisites

- `EvilCase.Host/.env` with the connection string and JWT key (copy from `.env.example`) — the host fails without them.
- Trusted dev certificate: `dotnet dev-certs https --trust`.

## Start

One server serves everything: `dotnet r run` → `https://localhost:5000` (Scalar UI at `/scalar`, Development only).

In Claude Code, prefer the preview server defined in `.claude/launch.json` (name `evilcase`).

## Verify

- Health: `curl.exe -sk https://localhost:5000/health/ready` → `{"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}`. `503` means the database is unreachable; `/health/live` answers even then.
- API round-trip: `curl.exe -sk -X POST https://localhost:5000/api/echo/post -H "Content-Type: application/json" -d '{"message":"ping"}'` → `{"message":"Echo: ping"}`
- Unknown API path: `curl.exe -sk -o /dev/null -w "%{http_code}" https://localhost:5000/api/nope` → `404`, never the app's HTML.
- Frontend: open `https://localhost:5000/echo`, type text, click Send → page shows `Echo: <text>`. First WebAssembly load takes a few seconds.

## Stop

Ctrl+C in the terminal, or stop the preview server.
