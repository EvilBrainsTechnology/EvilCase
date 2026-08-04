---
name: run-app
description: Run EvilCase locally — one host serving the API and the Blazor frontend — and verify the app works. Use for AI validation and testing of the running application.
---

# Run EvilCase

All commands run from `src/`; `dotnet tool restore` once per clone, because `r` is a local tool.

## Start

`dotnet r run-docker` builds the image from the working tree, runs it with its own PostgreSQL and
prints the address:

```bash
dotnet r run-docker    # EvilCase on http://localhost:32770, compose project evilcase-local-38f0ee10
```

Docker is the only prerequisite — no `.env`, no dev certificate, no database of one's own. The
port and the compose project belong to this checkout, so worktrees run side by side and nothing
of another agent's is ever touched. Read the address out of the output and verify against that
one, never a remembered address. An agent that started a stack removes it with
`dotnet r stop-docker` before it reports.

Every start rebuilds the image, so what runs is the working tree — about a minute per change. In
Claude Code, open the browser pane at the printed address; the preview server of
`.claude/launch.json` belongs to `dotnet r run`, which is the debugger's way in and carries the
prerequisites in `README.md`.

### Claude Code on the web

`.claude/hooks/session-start.sh` prepares the container at session start, only where
`CLAUDE_CODE_REMOTE` is `true`: it copies the SDK out of `mcr.microsoft.com/dotnet/sdk:10.0` (the
SDK installers are egress-blocked), installs `pwsh` from NuGet (`src/global.json` names it as
`scriptShell`), starts the container's own PostgreSQL, writes a `.env` with throwaway credentials
(`admin@evilcase.local` / `DevPassword123!`), trusts a dev certificate, and pins the repository's
commit identity to `claude[bot]` — worktrees share it. The hook restates the SDK version, the
connection string, the seed and JWT keys and the `src/` layout — a change to any of those is a
change to the hook, in the same commit.

Run the application there with `dotnet r run` → `https://localhost:5000`. Everything it needs is
in place, while an image build would pull from registries the egress policy does not cover. The
hook writes `.env` into the main checkout only, so a worktree has none: copy the file in.

## Verify

Against the address the start printed, `https://localhost:5000` where it was `dotnet r run`.

- `curl -sk <address>/health/ready` → `Healthy` with the `database` check; `503` means the
  database is unreachable, `/health/live` answers even then.
- Sign in: `POST /api/auth/login` with `{"email":…,"password":…}` (the seed values) →
  `accessToken`; `401` is bad credentials, `423` a lockout (5 failures, 15 minutes).
- API round-trip: `POST /api/echo/post` with the bearer → `Echo: …`; without the header `401`,
  which is the fallback policy working.
- `GET /api/nope` → `404`, never the app's HTML.
- Frontend: the address redirects to `/login`; sign in, open `/echo`, send a text. The first
  WebAssembly load takes a few seconds.

## Stop

`dotnet r stop-docker` removes the containers, the network and the database, which lives in RAM.
`dotnet r run` stops with Ctrl+C, or by stopping the preview server.
