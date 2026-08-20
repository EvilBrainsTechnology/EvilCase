# Agents

- Delegate every change to the repository; the main thread orchestrates and keeps the results.
- A slice runs the named agents from `.claude/agents/` through the slice-pipeline workflow:
  architect plans, coder implements through to the pull request, reviewer reviews and fixes.
  One review, no second round.
- Every subagent that writes to the repository gets `isolation: "worktree"`. A worktree has no
  `.env` (run-app skill); scratch files go under the session scratchpad.
- The main checkout never holds a delegated branch, `--ignore-other-worktrees` included.
- Independent tasks run in parallel, each with its own port and database, dropped afterwards
  (run-app skill).
- Search with `Grep`, `Glob` and `Read`. The shell is for commands that change something.
