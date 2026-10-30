---
name: product-loop
description: Run one round of the EvilCase product loop — start one slice, tend the open pull requests, report. Use for every round started from `.claude/loop.md`, the loop's entry point in this repository, and whenever the loop's schedule or its questions to the owner are in question. Unrelated to the global `loop` skill.
---

# EvilCase product loop

Move EvilCase toward `docs/product/vision.md`; ask the owner whatever is unclear, never permission.

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
`blocked` or `waiting-for-owner`: highest `Priority` (`Urgent`, `High`, `Medium`, `Low`, then none),
the lowest milestone breaking a tie, honouring a focus argument. Empty backlog: do nothing.

The round runs one slice at a time. An open pull request on a `loop/*` branch or a `loop` issue
labelled `agent-in-progress` stops the start: the round tends what is open and starts nothing. A
pull request on another branch never stops it. Otherwise take the first candidate, label it
`agent-in-progress`, and start one Workflow (`.claude/skills/product-loop/slice-pipeline.js`,
`args: [{issue, slug, title, body, fast, plan?, branch?}]`); it runs in the background — tend open pull requests
meanwhile. One slice is one pull request from database to UI leaving the app usable, small enough
to review on a phone, on `loop/<issue>-<slug>` off `master`. `fast: true` is the coder alone: no
behaviour change, no new test — a rename, doc wording, a sweep an analyzer verifies. Schema,
tenancy, security, an API contract, a screen, a test or a second project keep all three phases.

## 2. Ask, then wait

Every unclear point and every `question` a workflow returns goes to the owner in this chat
(`.claude/rules/agents.md`), with the issue or pull request it holds up. Its issue switches
`agent-in-progress` to `waiting-for-owner`, its pull request to `agent-done`; take something else.

An answer is the owner's reply in this chat. Apply it at once: remove `waiting-for-owner`, restart
the work with the question and the answer appended to its `body` or `instructions`. An answer
changing the vision or a governing SDD is an owner request: `touch .claude/allow-meta-edits`,
update it in the same commit as the code it governs, delete the flag. A round removes `blocked`
from an issue whose `Blocked by #` issues have all merged or closed.

## 3. Tend the open pull requests

- Tend only `loop/*` pull requests; the session that opened any other one tends it.
- A `waiting-for-agent` or `ci-failed` pull request on a free branch comes first (Between rounds).
- Answer every thread with no agent reply — read the threads, never filter by timestamp or
  count; a reply says what changed, or why not, in at most three sentences.
- A conflict is rebased onto `master` through a `pr-work.js` Workflow, never in the main thread.
- A pull request labelled `agent-in-progress` with no running workflow is taken over through
  a `pr-work.js` Workflow: finish the work and the threads, switch the label at the end.
- Never merge, however green or approved. Say nothing about waiting for one.
- Review another author's pull request only when it carries `request-code-review`.

## Between rounds

A `subscribe_pr_activity` notification is handled when it arrives, never left for the next round.
A merged pull request starts the next round at once. A question gets its reply and the switch to
`agent-done`; anything needing code sets `agent-in-progress` and starts one Workflow
(`.claude/skills/product-loop/pr-work.js`, `args: [{pr, branch, instructions, fast}]`) — one per
branch, only when no running workflow in `TaskList` names the branch. A red CI run (`ci-failed`)
and a hand-off comment naming a missing review take the same path, the latter fast. A rework is
fast unless a comment asks for a refactor or a change beyond the files it names.

## 4. Report

One short Czech chat message for the whole round, Prague times (`TZ=Europe/Prague date`): what is
open for review, what was fixed, which question waits on the owner. One line each; a question
is asked on its own (step 2), never in the report.

## The schedule

The clock is the fallback: an hourly session-bound Routine (`create_trigger`), never `CronCreate`.
A session that starts while the loop should run checks `list_triggers` and creates it if missing;
every turn ends confirming it is there. Repair a wrong one with `update_trigger`; delete only a
duplicate of the clock. On a usage limit, wait for the reset and resume.
