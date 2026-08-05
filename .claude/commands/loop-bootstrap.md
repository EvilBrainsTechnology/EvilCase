---
description: Set up the EvilCase product loop on GitHub — labels, milestones and a seed backlog derived from the vision
---

# Bootstrap the EvilCase product loop

Prepares GitHub for the loop from `docs/product/vision.md`: labels, its milestones, and one
issue per slice they name. Nothing here is copied from the vision — read it and derive, so a
new vision seeds a new backlog.

Safe to run again: every step lists what exists and creates only what is missing, because no
create call is idempotent on its own — a label that exists is an error, a milestone or an issue
with the same title a duplicate.

Calls go through `.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1`; every endpoint is in
`github-api.md` beside it.

## 1. Check the environment

Report each as OK or blocked, and stop at the first blocker rather than guessing:

- The script answers for `/repos/EvilBrainsTechnology/EvilCase -Select full_name` — one call, and
  the token reaches the repository.
- `dotnet tool restore` and `dotnet r ci` from `src/` pass.
- The app runs and the seeded administrator signs in — `.claude/skills/run-app/SKILL.md`.
- A screenshot of a signed-in screen can be taken — `.claude/skills/product-loop/visual-proof.md`.
  Report it blocked if the browsers are missing; the definition of done depends on it.

## 2. Labels

`labels` first, then `labels -Json '{"name":"…","color":"…"}'` for whichever is missing.

State: `loop` (work done by the loop), `needs-decision` (waiting on the owner), `decided`,
`blocked`. Area: one per milestone topic in the vision, plus `area/api` and `area/docs`, which
are cross-cutting rather than a milestone.

## 3. Milestones

`milestones?state=all` first, then `milestones -Json '{"title":"…"}'` for whichever titles are
missing. The vision's milestones, in its order.

## 4. Seed the backlog

One issue per slice the vision's milestones name, in that order, each on its milestone and
labelled `loop` plus its area. Body: what the slice ships, what "done" looks like in the UI, and
what it deliberately leaves out.

`issues?state=open` first (drop every element with a `pull_request` key);
skip a slice that is already open. A closed issue is history and never holds back a slice the
current vision asks for.

Do not open decision issues here — those come from the round that picks the slice up.

## 5. Report

The milestones and issues created, and the first slice the loop will take. Do not start
building — that is a round, started from `.claude/loop.md`.
