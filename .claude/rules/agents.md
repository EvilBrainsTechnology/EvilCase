# Agents

- Delegate every change to the repository; the main thread orchestrates and keeps the results.
- A slice runs the named agents from `.claude/agents/` through the slice-pipeline workflow:
  architect plans, coder implements through to the pull request, reviewer reviews and fixes.
  One review, no second round.
- An owner-approved proposal is the plan (`plan` arg): no architect, coder on Opus, effort high.
- The review runs beside the screenshots (`docs/loop/visual-proof.md`) and any security review.
  Once all finished, the orchestrator comments `.github/code_review_template.md`, then `agent-done`.
- A small change and an owner's rework take the fast lane: the coder alone (product-loop skill).
- Every subagent that writes to the repository gets `isolation: "worktree"`, with its own `.env`
  copy, port and database (run-app skill); scratch files go under the session scratchpad.
- The main checkout never holds a delegated branch, `--ignore-other-worktrees` included.
- Search the repository with `Grep`, `Glob` and `Read`, never the shell; the shell runs `git`,
  `gh` and the commands that build, run or change something.
