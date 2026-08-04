---
name: product-loop
description: Run one round of the EvilCase product loop — apply answered decisions, merge and clear open pull requests, ship thin vertical slices, report. Use for every round started from `.claude/loop.md`, the loop's entry point in this repository, and whenever the loop's schedule, its decision issues, its GitHub access or its stacked pull requests are in question. Unrelated to the global `loop` skill.
---

# EvilCase product loop

Move EvilCase toward `docs/product/vision.md`, one reviewable slice at a time. Never ask
permission to continue; the loop never waits for the owner. A round runs until nothing is
tractable, then reports everything once. Read the vision and the open work (§1, §2) first.

## GitHub access

`gh` is not installed. Use `curl` with `$GH_TOKEN` (base URL and endpoints in `github-api.md`
next to this file), never `mcp__github__*` tools — a fired round can wake without them. A
worktree refuses shell pipelines: write the calls into a scratchpad script and parse with
`python3`. The loop's own comments are recognised by author `claude[bot]` — measured, never assumed.

## Subagents

Delegation follows `.claude/rules/agents.md`; a round clears its dead delegations before it
reads anything else. The round's main thread holds three things itself: talking to the owner,
the schedule, and merging.

A subagent starts blank: the repository is the only memory. Before the round ends, a finding
becomes an issue, off-diff pull request state a comment on it, a new loop rule an instruction file.

## 1. Apply answered decisions

An answer is any comment on an open `needs-decision` issue whose author is not `claude[bot]`.
For each: state what was chosen in a `## Decision` comment, label `decided`, close, and remove
`blocked` from every issue referencing it. A decision that changes the vision updates it in the
same commit as the code it governs, or on its own when it governs none. Never answer or re-ask
an open decision.

## 2. Tend every open pull request

The round's first job and usually its whole job; take new work only when every open pull
request is merged, or green and answered.

- Merge what meets the gate in `.claude/rules/github.md` — mergeable by the repository's
  rules, into `master` only: squash, rebase what conflicts, wait for green
  CI, then the next that meets the gate.
- Outstanding means a review thread with no `claude[bot]` reply; never filter by timestamp or
  count — read the threads. Every comment is answered in the round that finds it: a requested
  change fixed to the full definition of done and answered with the commit, a question answered
  in the thread, a product decision turned into a decision issue linked from the thread.
- Then clear the rest: red CI, a conflict (rebase onto the target), a title or description
  that no longer matches the diff.

## 3. Pick the work

Take the highest-value open issue that is neither `blocked` nor `needs-decision`: landing on
`master` alone first, the lowest milestone second — no milestone defers nothing — honouring a
focus argument when one was given. Empty backlog → derive the next slice from the vision, open
its issue, take it. Everything blocked → one chat message re-surfacing the open questions, stop
the round. After a slice is open, come back here while the round has room.

## 4. Ask before building — generously

Every product, domain or UX branch with more than one reasonable answer gets both before any
code: a decision issue — `[DECISION] <question>`, label `needs-decision`, the blocked issue's
milestone, body with context, options with costs, a recommendation, `Blocks #<issue>` — and the
same question in the chat, a numbered choice answerable with one word. Label the dependent
issue `blocked`, carry on with the rest; technical choices under the rules are made silently.

## 5. Build one thin vertical slice

One pull request goes from database to UI and leaves the app usable. Branch
`loop/<issue>-<slug>`, off `master`; only when the slice cannot exist without an unmerged
branch, branch off that one and target it (see Stacks).

## 6. Definition of done

The pre-pull-request gate in `.claude/rules/github.md`, whole; a slice that cannot pass it
shrinks.

## 7. Pull request

Body: what changed, the screenshots, `Closes #<issue>`. A second layer of a chain links the
stack in the same step.

## 8. Report

One short Czech chat message for the whole round, Prague times (`TZ=Europe/Prague date`): what
shipped and merged with links, what was updated and why, what waits on which decision, what
comes next, the schedule's cadence — one line each. The report is never a question — anything
the owner must answer is a decision issue. The turn ends only after the schedule is confirmed.

## The schedule

The loop's clock is an hourly session-bound Routine (`create_trigger`), never `CronCreate` —
its jobs do not survive this environment. A session that starts while the loop should run
checks `list_triggers` and creates the Routine if missing; every turn ends confirming it is
still there. Repair a wrong Routine with `update_trigger`; delete only a duplicate, or the loop
the owner has ended; a denied Routine tool is a question for the owner, never worked around.
Re-subscribe (`subscribe_pr_activity`) to the open pull requests at session start and
unsubscribe on merge or close. The loop ends only when the owner says so or by the §3 stop,
both said out loud.

## Stacks

A slice needing an unmerged branch targets it; two or more such pull requests are linked as a
stack (calls in `github-api.md`). A stack is a cost: prefer slices that land on `master`,
shorten a chain from the bottom, never extend one when the work can exist without it. When the
bottom merges, GitHub retargets the next; rebase what conflicts onto the new `master`.

## Standing rules

- On a usage limit, wait for the reset and resume; never trade the definition of done for tokens.
- With two or more open pull requests, open nothing new and spend the round on those; the one
  exception is what the owner explicitly asked for.
