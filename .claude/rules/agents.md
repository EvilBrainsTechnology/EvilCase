# Agents

- Delegate the work to subagents, in the loop and in an ordinary session alike; the main
  thread orchestrates and keeps only the reports.
- A task is delegated whole — analysis, implementation, tests, validation, commits, push, the
  pull request, review replies — never analysis alone with the rest left to the main thread.
- A delegated implementation is followed by a code review from a fresh subagent; whoever
  delegated it works the relevant findings in before it reports and before the merge. An
  approval arriving while that review runs changes nothing — the merge waits for it.
- Independent tasks go out in parallel, and they share one machine: a subagent takes its own
  port and its own database rather than the defaults, and drops them afterwards (run-app skill).
- Search and read with `Grep`, `Glob` and `Read`; the shell is for commands that do something,
  not for looking. One `Grep` with a filter and a limit answers what a chain of `grep | head`
  costs several round trips to guess at.
- Every subagent that writes to the repository gets `isolation: "worktree"`; a worktree sees
  the parent checkout's rule files and has no `.env` (run-app skill).
- The main checkout never holds a delegated branch, `--ignore-other-worktrees` included: its
  working tree then reads as uncommitted changes that revert the agent's commits.
- A delegated task commits and pushes every unit as it finishes it — what never reaches the
  remote dies with the agent.
- Silence is not progress. Check every running delegation: no write in its worktree for twenty
  minutes is the signal, `TaskStop` answering `no task found` the proof. Remove a dead agent's
  worktree and its local branch — a stale worktree holds the branch checked out and the
  relaunch fails on it — and delegate the task again from whatever reached the remote.
- The exception is size, not kind: where spawning costs more than doing — one file to read, a
  one-line edit — do it directly.
