---
name: product-loop
description: Run one round of the EvilCase product loop — start one slice, tend the open pull requests, report. Use for every round started from `.claude/loop.md`, the loop's entry point in this repository, and whenever the loop's schedule or its decision issues are in question. Unrelated to the global `loop` skill.
---

# EvilCase product loop

Move EvilCase toward `docs/product/vision.md`; never ask permission and never wait for the owner.

## 1. Start the work, before anything else in the round

A round begins with `TaskList`, before the backlog is read. A running workflow is dead when its
task is over 3 hours old, or when every agent stopped writing:
`find ~/.claude/projects "${TMPDIR:-/tmp}/claude" -path '*EvilCase*' -name '*.jsonl' -mmin -15 2>/dev/null` prints nothing; a running agent appends to those transcripts. A dead workflow gets
`TaskStop`. A dead or failed run loses its worktrees and local branches. A remote `loop/*`,
`review/*` or `claude/*` branch goes only with no open pull request referencing it and its last
commit over 3 hours old — merged and closed pull requests' branches included. A round removes
`agent-in-progress` from any `loop` issue with no running workflow and no open pull request for
it. A failed run comments its issue; two such comments in a row label it `blocked`.

The backlog is the open issues labelled `loop` and none of `agent-in-progress` (taken),
`blocked` or `needs-decision`: highest `Priority` (`Urgent`, `High`, `Medium`, `Low`, then none),
the lowest milestone breaking a tie, honouring a focus argument. Empty backlog: do nothing.

The round runs one slice at a time. An open pull request on a `loop/*` branch or a `loop` issue
labelled `agent-in-progress` stops the start: the round tends what is open and starts nothing.
A pull request on another branch was requested, not started here, and never stops it. Otherwise
take the first candidate, label it `agent-in-progress`, and start one Workflow
(`.claude/skills/product-loop/slice-pipeline.js`, `args: [{issue, slug, title, body, fast}]`);
it runs in the background — tend open pull requests meanwhile. One slice is one pull request
from database to UI leaving the app usable, small enough to review on a phone, on
`loop/<issue>-<slug>` off `master`. `fast: true` is the coder alone: no behaviour change, no new
test — a rename, doc wording, a sweep an analyzer verifies. Schema, tenancy, security, an API
contract, a screen, a test or a second project keep all three phases.

## 2. Decide, do not ask

Choose the reasonable answer yourself and state it in the pull request description as one
sentence. Open a decision issue, filling `.github/ISSUE_TEMPLATE/decision.md`, only where being
wrong is expensive to undo: the database schema, the domain model, security. Label the
dependent issue `blocked` and take something else.

An answer is a comment from the owner, `vdolek`, on an open `needs-decision` issue. Apply it:
say what was chosen, label `decided`, close it, and unblock what referenced it. A decision that
changes the vision or a governing SDD is an owner request: `touch .claude/allow-meta-edits`,
update it in the same commit as the code it governs, delete the flag. A round removes `blocked`
from an issue whose `Blocked by #` issues have all merged or closed.

## 3. Tend the open pull requests

- A `waiting-for-agent` or `ci-failed` pull request on a free branch comes first (Between rounds).
- Answer every thread with no agent reply — read the threads, never filter by timestamp or
  count; a reply says what changed, or why not, in at most three sentences.
- A conflict is rebased onto `master` through a `pr-work.js` Workflow, never in the main thread.
- A pull request labelled `agent-in-progress` with no running workflow is taken over through
  a `pr-work.js` Workflow: finish the work and the threads, switch the label at the end.
- Never merge, however green or approved. Say nothing about waiting for one.
- Review another author's pull request only when it carries `request-code-review`.

## Between rounds

A `subscribe_pr_activity` notification is handled when it arrives, never left for the next
round. A question gets its reply and the switch to `agent-done`; anything needing code sets
`agent-in-progress` and starts one Workflow (`.claude/skills/product-loop/pr-work.js`,
`args: [{pr, branch, instructions, fast}]`) — one per branch, only when no running workflow
in `TaskList` names the branch. A red CI run flags `ci-failed` and takes the same path.

## 4. Report

One short Czech chat message for the whole round, Prague times (`TZ=Europe/Prague date`): what is
open for review, what was fixed, what waits on the owner. One line each, never a question.

## The schedule

The clock is an hourly session-bound Routine (`create_trigger`), never `CronCreate`. A session
that starts while the loop should run checks `list_triggers` and creates it if missing; every
turn ends confirming it is there. Repair a wrong one with `update_trigger`; delete only a
duplicate of the clock. On a usage limit, wait for the reset and resume.
