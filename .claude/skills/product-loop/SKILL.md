---
name: product-loop
description: Run one round of the EvilCase product loop — start independent slices in parallel, tend the open pull requests, report. Use for every round started from `.claude/loop.md`, the loop's entry point in this repository, and whenever the loop's schedule or its decision issues are in question. Unrelated to the global `loop` skill.
---

# EvilCase product loop

Move EvilCase toward `docs/product/vision.md`, one reviewable slice at a time. Never ask
permission to continue and never wait for the owner. Delegation follows
`.claude/rules/agents.md`; GitHub follows `.claude/rules/github.md`.

The loop never changes `.claude/**` and never merges. A rule that is missing or wrong becomes an
issue for the owner.

## 1. Start the work

Do this first, so the machine is busy while the round does everything else.

The backlog is the open issues labelled `loop`. Among those neither `blocked` nor
`needs-decision`, take the highest `Priority` field — `Urgent`, `High`, `Medium`, `Low`, then the
ones carrying none — the lowest milestone breaking a tie, honouring a focus argument when one was
given. Empty backlog: derive the next slice from the vision and open its issue.

Take two or three that do not touch the same files and delegate them in parallel, each a whole
task in its own worktree. One slice is one pull request from database to UI that leaves the app
usable, on `loop/<issue>-<slug>` off `master`. A slice that needs another's unmerged branch is
not started.

## 2. Decide, do not ask

Choose the reasonable answer yourself and state it in the pull request description as one
sentence. Open a `[DECISION] <question>` issue, labelled `needs-decision` with `Blocks #<issue>`,
only where being wrong is expensive to undo: the database schema, the domain model, security.
Label the dependent issue `blocked` and take something else.

An answer is any comment on an open `needs-decision` issue that carries no agent footer. Apply
it: say what was chosen, label `decided`, close it, and unblock what referenced it. A decision
that changes the vision updates it in the same commit as the code it governs.

## 3. Tend the open pull requests

- Answer every review comment in the round that finds it. Outstanding means a thread with no
  agent reply — read the threads, never filter by timestamp or count.
- Rebase onto `master` on a conflict; fix red CI; correct a title or description that no longer
  matches the diff.
- Never merge, however green or approved. Say nothing about waiting for one.

## 4. Report

One short Czech chat message for the whole round, Prague times (`TZ=Europe/Prague date`): what is
open for review, what was fixed, what waits on the owner. One line each, never a question.

## The schedule

The clock is an hourly session-bound Routine (`create_trigger`), never `CronCreate`. A session
that starts while the loop should run checks `list_triggers` and creates it if missing; every
turn ends confirming it is there. Repair a wrong one with `update_trigger`; delete only a
duplicate. On a usage limit, wait for the reset and resume.
