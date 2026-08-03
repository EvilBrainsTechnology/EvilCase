---
description: One-off setup of the EvilCase product loop — checks, labels, milestones, seed backlog
---

# Bootstrap the EvilCase product loop

Run once, and safe to run again: every step below lists what exists before it creates anything and
creates only what is missing. None of the create calls is idempotent on its own — a label that
exists is an error, a milestone or an issue with the same title is a duplicate — so the listing is
the mechanism, not a precaution.

`gh` is not installed; `api.github.com` is reachable with `$GH_TOKEN`.
Every call below is a `curl`, as described in `.claude/skills/product-loop/SKILL.md`:

```bash
GH=https://api.github.com/repos/EvilBrainsTechnology/EvilCase
curl -s -H "Authorization: Bearer $GH_TOKEN" -H "Accept: application/vnd.github+json" "$GH/labels?per_page=100"
```

## 1. Check the environment

Report each as OK or blocked, and stop at the first blocker rather than guessing:

- `GET $GH` answers `200` — the token reaches `EvilBrainsTechnology/EvilCase`.
- `dotnet tool restore` from `src/`, then `dotnet r build`.
- `docker compose -f deploy/docker-compose.dev.yml up -d --wait` from the repository root brings up
  PostgreSQL on the connection string `.env.example` already carries.
- `src/EvilCase.Host/.env` exists and carries `EvilBrains__EvilCase__Auth__Seed__Email` and
  `__Password` — without the seeded administrator there is no way into the application, and the
  visual proof below is impossible.
- The `evilcase` preview server (`.claude/launch.json`, `https://localhost:5000` — see
  `.claude/skills/run-app/SKILL.md`) reaches `/health/ready` as `Healthy`, and the seeded
  administrator can sign in. An instance already running from the IDE holds that port and has to be
  stopped first.
- A screenshot of a signed-in screen can be taken with Playwright, as
  `.claude/skills/product-loop/SKILL.md` describes under *Visual proof*. Report it blocked if the
  browsers are missing; the definition of done there depends on it.

## 2. Labels

`GET $GH/labels?per_page=100` first, then `POST $GH/labels` `{"name":"…","color":"…"}` for whichever
of these is missing.

State: `loop` (work done by the loop), `needs-decision` (waiting on the owner), `decided`, `blocked`.

Area, one per milestone topic so every issue below has one: `area/domain`, `area/ui`, `area/import`,
`area/timeline`, `area/deadlines`, `area/search`, `area/templates`, `area/users`. Two more are
cross-cutting rather than a milestone: `area/api` and `area/docs`.

## 3. Milestones

`GET $GH/milestones?state=all` first, then `POST $GH/milestones` `{"title":"…"}` for whichever titles
are missing — GitHub accepts a duplicate title without complaint.

M0 Domain core, M1 Case UI, M2 Acts UI, M3 Import, M4 Timeline, M5 Deadlines, M6 Search,
M7 Templates, M8 Ownership and users — as described in `docs/product/vision.md`.

## 4. Seed the backlog

`GET $GH/issues?state=all&per_page=100` first (drop every element with a `pull_request` key); a slice
whose issue already exists is skipped, and nothing here is ever opened twice.

One issue per slice below, in this order, each on its milestone, each labelled `loop` and its area
(`POST $GH/issues`). Body: what the slice ships, what "done" looks like in the UI, and what it
deliberately leaves out. Do not open decision issues here — those come from the round that picks the
slice up.

**M0** — case aggregate with owner, sub-case tree, status and tags · party (authority, official,
person) · case references, one internal mark plus N external marks bound to a party · act with
ordinal, direction, dates and the issuer's file number · content-addressed file asset with
role-carrying links to acts · comment on case and act.

**M1** — case list with search · case detail with the sub-case tree · create and edit a case.

**M2** — act list inside case detail · add an act with file upload by role · act detail with its
files and comments.

**M3** — folder-convention parser as a pure unit-tested function over synthetic fixtures ·
dry-run preview of an import · import execution with dedupe by content hash.

**M4** — merged timeline read model over a case and its descendants · timeline UI with filters.

**M5** — deadline model and derivation from a delivery date · cross-case "what is due" view.

**M6** — text extraction from PDF and DOCX into a search index · fulltext search UI · manual
summary field per document, with the AI slot left unwired.

**M7** — `.docx` template model and its placeholder set · generate a submission pre-filled from
the case.

**M8** — ownership enforced on every query and endpoint · user management on top of the existing
seeded administrator · the seam for later multi-tenancy.

## 5. Report

Print the milestone and issue numbers created, and the first slice the loop will take. Do not start
building — that is a round, started from `.claude/loop.md`.
