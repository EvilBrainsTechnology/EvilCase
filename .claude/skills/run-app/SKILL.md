---
name: run-app
description: Run the EvilCase API and Blazor frontend locally and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`.

## Prerequisites

- `Api/EvilCase.Api/.env` with the connection string and JWT key (copy from `.env.example`) — the API fails without them.
- Trusted dev certificate: `dotnet dev-certs https --trust`.

## Start

Both servers must run simultaneously:

- API: `dotnet r run-api` → `https://localhost:5000` (Scalar UI at `/scalar`, Development only)
- Frontend: `dotnet r run-app` → `https://localhost:5001`

In Claude Code, prefer the preview servers defined in `.claude/launch.json` (names `api` and `app`).

## Verify

- Health: `curl.exe -sk https://localhost:5000/health/ready` → `{"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}`. `503` means the database is unreachable; `/health/live` answers even then.
- API round-trip: `curl.exe -sk -X POST https://localhost:5000/echo/post -H "Content-Type: application/json" -d '{"message":"ping"}'` → `{"message":"Echo: ping"}`
- Frontend: open `https://localhost:5001`, type text, click Send → page shows `Echo: <text>`. First WebAssembly load takes a few seconds.
- CORS error in the browser console means the API is not running or the origin does not match (the dev CORS policy allows `https://localhost:5001`).

## Stop

Ctrl+C in each terminal, or stop the preview servers.
