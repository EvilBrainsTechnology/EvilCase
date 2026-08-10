---
name: product-loop
description: Run one round of the EvilCase product loop — start independent slices in parallel, tend the open pull requests, report. Use for every round started from `.claude/loop.md`, the loop's entry point in this repository, and whenever the loop's schedule or its decision issues are in question. Unrelated to the global `loop` skill.
---

# EvilCase product loop

Move EvilCase toward `docs/product/vision.md`, one reviewable slice at a time. Never ask
permission to continue and never wait for the owner.

## 1. Start the work, before anything else in the round

A round begins with `TaskList`, before the backlog is read. A running workflow is dead when its
task transcript file's mtime is over 15 minutes old or the task is over 3 hours old; one still
writing runs on. A dead workflow gets `TaskStop`; a dead or failed run loses worktrees, local
branches and, when no open pull request references it, the remote `loop/<issue>-…` branch
before any retry. A failed run comments its issue; two such comments in a row label it `blocked`.

The backlog is the open issues labelled `loop`, neither `blocked` nor `needs-decision`: highest
`Priority` (`Urgent`, `High`, `Medium`, `Low`, then none), the lowest milestone breaking a tie,
honouring a focus argument. Empty backlog: do nothing.

Take two or three and start one Workflow (`.claude/skills/product-loop/slice-pipeline.js`,
`args: [{issue, slug, title, body, plan}, …]`). `plan` is true for a slice with a migration, a
new entity, a change across layers or a touch on security; false otherwise. One slice is one
pull request from database to UI that leaves the app usable, on `loop/<issue>-<slug>` off
`master`. The workflow runs in the background; tend the open pull requests meanwhile and take
its returned results into the report — the details stay inside the workflow.

Two slices never touch the same files, and every open pull request counts — a candidate is
checked against the changed files of every open pull request. A migration collides with every
other migration. An issue whose `loop/<issue>-…` branch already exists, locally or on the
remote, is already taken. A slice that needs another's unmerged branch is not started. Where
nothing clears this, the round starts nothing and tends what is open.

## 2. Decide, do not ask

Choose the reasonable answer yourself and state it in the pull request description as one
sentence. Open a decision issue, filling `.github/ISSUE_TEMPLATE/decision.md`, only where being
wrong is expensive to undo: the database schema, the domain model, security. Label the
dependent issue `blocked` and take something else.

An answer is a comment from the owner, `vdolek`, on an open `needs-decision` issue. Apply it:
say what was chosen, label `decided`, close it, and unblock what referenced it. A decision that
changes the vision or a governing SDR updates it in the same commit as the code it governs.
A round removes `blocked` from an issue whose blockers, read from the `Blocked by #` line in
its body, have all merged or closed.

## 3. Tend the open pull requests

- A `waiting-for-agent` or `ci-failed` pull request comes first, handled per Between rounds.
- Answer every review comment in the round that finds it. Outstanding means a thread with no
  agent reply — read the threads, never filter by timestamp or count. A comment is at most
  three sentences; a reply to a review one says what changed, or why not.
- A conflict gets its rebase onto `master` through a `pr-work.js` Workflow, never in the main
  thread; correct a title or description that no longer matches the diff.
- A pull request labelled `agent-in-progress` with no running workflow is taken over through
  a `pr-work.js` Workflow: finish the work and the threads, switch the label at the end.
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
duplicate of the clock. On a usage limit, wait for the reset and resume.
