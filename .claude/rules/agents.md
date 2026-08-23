# Agents

- Delegate every change to the repository; the main thread orchestrates and keeps the results.
- A slice runs the named agents from `.claude/agents/` through the slice-pipeline workflow:
  architect plans, coder implements through to the pull request, reviewer reviews and fixes.
  One review, no second round.
- A small change takes the fast lane — the coder alone; the product-loop skill defines it.
- Every subagent that writes to the repository gets `isolation: "worktree"`. Worktrees run in
  parallel, each with its own `.env` copy, port and database (run-app skill); scratch files
  go under the session scratchpad.
- The main checkout never holds a delegated branch, `--ignore-other-worktrees` included.
- Search with `Grep`, `Glob` and `Read`, never the shell; the shell runs `git`, `gh` and the
  commands that build, run or change something.
