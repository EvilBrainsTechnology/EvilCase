---
description: Run one iteration of the EvilCase product loop
argument-hint: [optional focus — an issue number or an area]
---

# EvilCase product loop — one iteration

Move EvilCase from its proof-of-concept skeleton toward `docs/product/vision.md`, one reviewable
slice at a time. This command is exactly one iteration: finish it and report. Never ask for
permission to continue.

Read every iteration, before anything else: `docs/product/vision.md`, `AGENTS.md`, and the open
work — `gh issue list --state open --limit 100 --json number,title,labels,milestone`.

Focus: $ARGUMENTS

## 1. Apply answered decisions

List `gh issue list --label needs-decision --state open` and read the comments of each. Any comment
authored by the repository owner is the answer, even if it is one word or picks no listed option.

For each answered decision: append a `## Decision` comment stating what was chosen and what follows
from it, label it `decided`, close it, and remove `blocked` from every issue that references it. If
the decision changes the vision, `docs/product/vision.md` is updated in the next pull request, in
the same commit as the code it governs.

Unanswered decisions stay open. Do not re-ask them and do not answer them yourself.

## 2. Pick the work

Take the single highest-value open issue that is neither `blocked` nor `needs-decision`, preferring
the lowest open milestone. Honour the focus argument if one was given.

- Backlog empty → derive the next thin vertical slice from the vision, open an issue for it, take it.
- Every open issue blocked → do not idle and do not guess. Post one chat message re-surfacing the
  open questions with their issue links, and stop the iteration.

## 3. Ask before building — generously

The owner wants to be consulted often. Before writing code, list every product, domain or UX branch
in this slice that has more than one reasonable answer, and ask about each one. Technical choices
already governed by `AGENTS.md` — naming, layering, test structure, EF details — you make yourself,
silently, without asking.

For each question, do both:

**Open a decision issue.** Title `[DECISION] <the question, short>`, label `needs-decision`, same
milestone as the blocked issue, body:

```
## Context
Two or three sentences. What the slice is, why this branch exists.

## Options
### A — <name>  (recommended)
What it means. What it costs. What it makes impossible later.
### B — <name>
...

## Recommendation
A, because ...

## Blocks
#<issue>
```

**Ask the same question in the chat**, under ten lines, as a numbered choice with a recommendation,
so it is answerable from a phone with one word.

Then label the dependent issue `blocked`, link the decision, and carry on with whatever is not
blocked by it — a smaller part of the slice, or the next issue. A question never stops the loop.

## 4. Build one thin vertical slice

One pull request goes from database to UI and leaves the app usable. Small enough to review on a
phone. Branch `loop/<issue>-<slug>` off `master`, and commit in logical units as the work proceeds,
so the branch reads as a sequence of steps rather than one lump.

`AGENTS.md` is binding, without exception — the API client generator, the controller conventions,
the analyzers at error severity, `internal sealed` behind interfaces, no `Async` suffix, the
responsive rules, English in the repository and Czech in the UI strings.

Authentication already ships and is default-deny. Every new endpoint is authenticated and every new
page sits inside `MainLayout`, therefore protected. Adding `[AllowAnonymous]` anywhere, or placing
a page outside `MainLayout`, is a decision issue, never a silent choice. Every new aggregate root
carries its owner from its first migration.

## 5. Definition of done

All four hold before the pull request exists:

- `dotnet r ci` green, run from `src/`.
- New tests covering what the slice adds, not only that it builds.
- Visual proof: start PostgreSQL
  (`docker compose -f deploy/docker-compose.dev.yml up -d --wait` from the repository root), start
  the `evilcase` preview server (`.claude/launch.json`, port 5100 — see `.claude/skills/run-app`),
  sign in as the seeded administrator, then screenshot every changed screen at 1440×900 and at
  390×844 and attach both to the pull request.
- Documentation updated in the same commit: `AGENTS.md` for cross-cutting rules, the README next
  to the code for implementation detail.

A red gate is fixed, never worked around. Analyzers are not suppressed to pass. If the slice cannot
meet the gate, shrink the slice.

## 6. Pull request

`gh pr create`, TL;DR on the first line, then what changed, the screenshots, and `Closes #<issue>`.
Never merge. Never push to `master`. Merging is the owner's.

## 7. Report

One short chat message: what shipped and its pull request link, what now waits on the owner with
decision links, what comes next. Then the iteration is over.

## Standing rules

- Real case folders on the owner's disk are read-only reference. Never write there, never copy a
  real document into the repository. Fixtures are synthetic files mimicking the naming convention.
- The repository is public. No real case content, names, file marks or personal data anywhere —
  code, tests, docs, issues, pull requests, commit messages.
- On a usage limit, wait for the reset and resume at the same point. Never trade the definition of
  done for tokens.
- Prefer reversible steps. A destructive migration, a dependency change, a change to authentication
  or the security headers, a rewrite of something that already works: ask first, as a decision issue.
- One iteration is one pull request at most. Breadth comes from many iterations, not from big ones.
