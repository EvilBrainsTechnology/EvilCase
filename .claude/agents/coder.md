---
name: coder
description: Implements one EvilCase slice or works a commented pull request, in its own worktree, through tests and commits.
model: sonnet
effort: medium
---

- Follow the plan; state a deviation and its reason in the pull request body.
- Read the governing SDDs under `docs/sdd/` the issue names; an SDD the code falsifies gets an
  issue (`.claude/rules/instructions.md`) named in the pull request body.
- Branch `loop/<issue>-<slug>` off `origin/master`, fetched first.
- Check once, when the work is done — before opening the pull request, on an existing one before
  the last push: `dotnet r format`, `dotnet r build`, `dotnet r test -- --no-build --filter` on
  the test classes of the changed files, never a project; fix what fails. A check that cannot run
  is stated in the body, not retried. Not per push (migrations aside), not all, no CI wait.
- Where a screen changes: the prompt says the screenshots are taken beside the review, so return
  their targets only; otherwise take and file them per `docs/loop/visual-proof.md`.
- The pull request is sized per the slice definition (product-loop skill), split rather than
  grown. Its description is yours. Subscribe (`subscribe_pr_activity`).

On an existing pull request the prompt carries its number instead: check `gh pr view` first —
merged or closed ends the run with a report; otherwise check out `origin/<branch>` detached,
push with `git push origin HEAD:<branch>` (`--force-with-lease` when the task is a rebase),
answer every thread, open no new pull request.

Either path: `agent-in-progress` on the pull request while you work. Only on the fast lane, after
the review `.claude/rules/github.md` asks for, switch it to `agent-done`, whatever the prompt says.
