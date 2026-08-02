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

List `gh issue list --label needs-decision --state open` and read the comments of each. **Any
comment on an open decision issue is the owner's answer**, even if it is one word or picks no listed
option.

That rule holds only because of this one, which must not be relaxed: **the loop never comments on a
decision issue while it is open.** `gh` is authenticated as the owner's own account, so a comment
the loop writes is indistinguishable from an answer and the next iteration would read its own text
as a decision. Everything the loop has to say about the question goes in the issue body at the time
it opens it. Needing to add something later means editing the body (`gh issue edit`), never
commenting.

For each answered decision: state what was chosen and what follows from it in a `## Decision`
comment — the one comment the loop is allowed, because it posts it and closes the issue in the same
step — then label it `decided`, close it, and remove `blocked` from every issue that references it.
If the decision changes the vision, `docs/product/vision.md` is updated in the next pull request,
in the same commit as the code it governs; a decision that governs no code updates it on its own.

Unanswered decisions stay open. Do not re-ask them and do not answer them yourself.

## 2. Get what is open merged, before opening anything new

`gh pr list --author @me --state open --json number,title`. For each, read the reviews and
comments (`gh pr view <n> --json reviews,comments` and
`gh api repos/{owner}/{repo}/pulls/<n>/comments`) and find anything from the owner that
arrived after the branch's last commit.

**This is the round's first job and usually its whole job.** The measure of the loop is what reaches
`master`, not how many branches it has open. An unmerged pull request holds up every slice above it,
goes stale against a moving base, and costs the owner more to review the longer it waits — so a round
that answers three review threads and rebases two branches has done more than one that opens a fourth
pull request.

Every comment gets a response in the same round it is found. Nothing is deferred to "next time".

- A requested change → check the branch out, fix it, meet the whole definition of done again,
  push, and reply in the thread saying what changed and in which commit.
- A question → answer in the thread, no code.
- A product decision in disguise → open a decision issue, link it from the thread, leave the
  pull request open.

Then clear what stands between the rest and a merge, in this order: a red CI run, a merge conflict or
a stale base (rebase it), a pull request whose description no longer matches its diff. A branch that
is green, current and answered is ready for the owner, and saying so in the report is what gets it
merged.

Take new work only when every open pull request is in that state.

## 3. Pick the work

Take the single highest-value open issue that is neither `blocked` nor `needs-decision`, preferring
the lowest open milestone. Honour the focus argument if one was given.

**Between two candidates, take the one that lands on `master` on its own.** A slice that needs an
unmerged branch adds a layer to something already waiting; a slice that does not is one the owner can
merge on its own the day it is opened. Depth is only worth it when the work genuinely cannot exist
otherwise — see *Stacked pull requests* in `AGENTS.md`. If every remaining candidate would deepen a
stack, say so in the report rather than deepening it by default.

- Backlog empty → derive the next thin vertical slice from the vision, open an issue for it, take it.
- Every open issue blocked → do not idle and do not guess. Post one chat message re-surfacing the
  open questions with their issue links, and stop the iteration.

## 4. Ask before building — generously

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

## 5. Build one thin vertical slice

One pull request goes from database to UI and leaves the app usable. Small enough to review on a
phone. Branch `loop/<issue>-<slug>`, and commit in logical units as the work proceeds, so the branch
reads as a sequence of steps rather than one lump.

Off `master` when the slice needs nothing that is still open. When it builds on an unmerged branch,
branch off that one instead and target it — see *Stacked pull requests* in `AGENTS.md`, which is
binding. Never wait for a merge that is the owner's to make; stacking is how the loop keeps moving
without one.

`AGENTS.md` is binding, without exception — the API client generator, the controller conventions,
the analyzers at error severity, `internal sealed` behind interfaces, no `Async` suffix, the
responsive rules, English in the repository and Czech in the UI strings.

Authentication already ships and is default-deny. Every new endpoint is authenticated and every new
page sits inside `MainLayout`, therefore protected. Adding `[AllowAnonymous]` anywhere, or placing
a page outside `MainLayout`, is a decision issue, never a silent choice. Every new aggregate root
carries its owner from its first migration.

## 6. Definition of done

All four hold before the pull request exists:

- `dotnet r ci` green, run from `src/`.
- New tests covering what the slice adds, not only that it builds.
- Visual proof, as described below.
- Documentation updated in the same commit: `AGENTS.md` for cross-cutting rules, the README next
  to the code for implementation detail.

A red gate is fixed, never worked around. Analyzers are not suppressed to pass. If the slice cannot
meet the gate, shrink the slice.

### Visual proof

Start PostgreSQL (`docker compose -f deploy/docker-compose.dev.yml up -d --wait` from the repository
root), start the `evilcase` preview server (`.claude/launch.json` — see `.claude/skills/run-app`),
sign in as the seeded administrator, and screenshot every changed screen at 1440×900 and at 390×844,
the two sides of the `lg` breakpoint.

`gh` cannot upload an image — GitHub's own attachment upload exists only in the web interface — so
the screenshots reach the pull request as committed files:

- Save them as `docs/screenshots/<issue>/<screen>-<width>.png` and commit them with the slice.
- Embed them in the pull request body by raw URL pinned to that commit, which resolves before the
  branch is merged and after it is deleted:
  `https://raw.githubusercontent.com/EvilBrainsTechnology/EvilCase/<sha>/docs/screenshots/...`
- A slice that replaces a screen deletes the screenshots it supersedes in the same pull request, so
  the directory stays the current state of the application rather than its history.

Everything in them is synthetic, by the standing rule below.

## 7. Pull request

`gh pr create`, TL;DR on the first line, then what changed, the screenshots, and `Closes #<issue>`.
Against `master`, or against the branch this one was built on. Then `subscribe_pr_activity` on it, so
its comments and CI arrive in the session rather than waiting for a round.

If it is the second pull request of a chain, link the chain as a stack on GitHub in the same step —
the base branches alone leave the order implicit and every reviewer to work it out. `AGENTS.md` has
the calls, including the one that dissolves a stack and must never be used to find out whether one
exists.

Then stop. **The loop never merges the pull request and never pushes to `master`** — not its own,
not after a green CI run, not because the iteration would otherwise look unfinished. `AGENTS.md`
makes that binding for every agent; the loop is the one most likely to be tempted, because it runs
unattended and merging is the only thing standing between it and the next slice. It waits instead.

## 8. Report

One short chat message: what shipped and its pull request link, every pull request this iteration
updated in answer to review feedback and what changed in it, what now waits on the owner with
decision links, what comes next. Then the iteration is over.

**The report is a report, never a question.** It ends by saying what the next round will take, and the
loop takes it. Anything the owner has to answer is a decision issue, linked from the report and never
a question in the chat that the loop then waits on — a round that ends waiting is a round that ended
early. The owner reads the report to know what happened, not to unblock anything.

The round is over when the report is written. **The turn is over when the next round is scheduled** —
check the recurring schedule is still there and name its cadence in the report, per *Keeping the
product loop running* in `AGENTS.md`. That check belongs to every turn, including one that
interrupted a round to ask about something else entirely; a loop that stops does so silently.

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
- **The loop never waits for the owner.** It opens the decision issue and carries on with whatever is
  not blocked by it. It does not pause for approval to commit, to open a pull request, to close its
  own pull request, or to pick the next slice — those are the loop's to make, and the owner reverses
  any of them at leisure. The only genuine stop is the one in *Pick the work*: every open issue
  blocked, nothing buildable, say so once and go quiet.
