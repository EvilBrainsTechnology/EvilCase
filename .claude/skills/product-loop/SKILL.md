---
name: product-loop
description: Run one round of the EvilCase product loop — start independent slices in parallel, tend the open pull requests, report. Use for every round started from `.claude/loop.md`, the loop's entry point in this repository, and whenever the loop's schedule or its decision issues are in question. Unrelated to the global `loop` skill.
---

# EvilCase product loop

Move EvilCase toward `docs/product/vision.md`, one reviewable slice at a time. Never ask
permission to continue and never wait for the owner.

## 1. Start the work

Do this before anything else in the round.

The backlog is the open issues labelled `loop`. Among those neither `blocked` nor
`needs-decision`, take the highest `Priority` field — `Urgent`, `High`, `Medium`, `Low`, then the
ones carrying none — the lowest milestone breaking a tie, honouring a focus argument when one was
given. Empty backlog: do nothing.

Take two or three and start one Workflow:
`scriptPath: .claude/skills/product-loop/slice-pipeline.js`,
`args: [{issue, slug, title, body, plan}, …]`. `plan` is true for a slice with a migration, a
new entity, a change across layers or a touch on security; false otherwise. One slice is one
pull request from database to UI that leaves the app usable, on `loop/<issue>-<slug>` off
`master`. The workflow runs in the background; tend the open pull requests meanwhile and take
its returned results into the report — the details stay inside the workflow.

Two slices never touch the same files, and every open pull request counts, not only the ones this
round started — a candidate is checked against the changed files of every open pull request. A
migration collides with every other migration, whatever it changes. A slice that needs another's
unmerged branch is not started. Where nothing left clears this, the round starts nothing and
tends what is open.

A workflow whose agents wrote nothing to their worktrees for twenty minutes is dead: `TaskStop`,
remove the worktrees and local branches, start it again — the remote holds nothing yet.

## 2. Decide, do not ask

Choose the reasonable answer yourself and state it in the pull request description as one
sentence. Open a `[DECISION] <question>` issue, labelled `needs-decision` with `Blocks #<issue>`,
only where being wrong is expensive to undo: the database schema, the domain model, security. An
issue body is at most 800 characters. Label the dependent issue `blocked` and take something
else.

An answer is any comment on an open `needs-decision` issue that is not the agent's. Apply
it: say what was chosen, label `decided`, close it, and unblock what referenced it. A decision
that changes the vision updates it in the same commit as the code it governs.

## 3. Tend the open pull requests

- A `waiting-for-agent` or `ci-failed` pull request comes first, handled per Between rounds.
- Answer every review comment in the round that finds it. Outstanding means a thread with no
  agent reply — read the threads, never filter by timestamp or count. A comment is at most
  three sentences; a reply to a review one says what changed, or why not.
- Rebase onto `master` on a conflict; correct a title or description that no longer matches the
  diff.
- A pull request labelled `agent-in-progress` with no running workflow gets its review finished
  here, label switched at the end.
- Never merge, however green or approved. Say nothing about waiting for one.
- Review another author's pull request only when it carries `request-code-review`.

## Between rounds

A `subscribe_pr_activity` notification is handled when it arrives, never left for the next
round. Triage the comments: a question gets its reply and the switch to `agent-done`; anything
needing code sets `agent-in-progress` and starts one Workflow
(`.claude/skills/product-loop/pr-work.js`, `args: [{pr, branch, instructions, full}]`); `full`
— rework, migration, new entity, cross-layer, security — adds the architect and the reviewer.

## 4. Report

One short Czech chat message for the whole round, Prague times (`TZ=Europe/Prague date`): what is
open for review, what was fixed, what waits on the owner. One line each, never a question.

## The schedule

The clock is an hourly session-bound Routine (`create_trigger`), never `CronCreate`. A session
that starts while the loop should run checks `list_triggers` and creates it if missing; every
turn ends confirming it is there. Repair a wrong one with `update_trigger`; delete only a
duplicate. On a usage limit, wait for the reset and resume.
