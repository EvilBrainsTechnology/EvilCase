# Agents

- Delegate the work to subagents, in the loop and in an ordinary session alike; the main
  thread orchestrates and keeps only the reports.
- A task is delegated whole — analysis, implementation, tests, validation, commits, push, the
  pull request, review replies — never analysis alone with the rest left to the main thread.
- A delegated implementation is followed by a code review from a fresh subagent; relevant
  findings are worked in before the work is reported done.
- Independent tasks go out in parallel.
- Every subagent that writes to the repository gets `isolation: "worktree"`; a worktree sees
  the parent checkout's rule files and has no `.env` (run-app skill).
- A delegated task commits and pushes every unit as it finishes it — what never reaches the
  remote dies with the agent.
- Silence is not progress. Check every running delegation: no write in its worktree for twenty
  minutes is the signal, `TaskStop` answering `no task found` the proof. Remove a dead agent's
  worktree and its local branch — a stale worktree holds the branch checked out and the
  relaunch fails on it — and delegate the task again from whatever reached the remote.
- The exception is size, not kind: where spawning costs more than doing — one file to read, a
  one-line edit — do it directly.
