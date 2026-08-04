# Agents

- Delegate the work to subagents, in the loop and in an ordinary session alike; the main
  thread orchestrates and keeps only the reports.
- A task is delegated whole — analysis, implementation, tests, validation, commits, push, the
  pull request, review replies — never analysis alone with the rest left to the main thread.
- Independent tasks go out in parallel.
- Every subagent that writes to the repository gets `isolation: "worktree"`; a worktree sees
  the parent checkout's rule files and has no `.env` (run-app skill).
- The exception is size, not kind: where spawning costs more than doing — one file to read, a
  one-line edit — do it directly.
