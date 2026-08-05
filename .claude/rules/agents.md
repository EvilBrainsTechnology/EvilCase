# Agents

- Delegate repository work to subagents; the main thread orchestrates and keeps the reports.
- A task is delegated whole: analysis, implementation, tests, commits, push, pull request.
- One code review follows the implementation, by a fresh subagent. The reviewer fixes what it
  finds, in the same run. There is no second round.
- What the reviewer is unsure about goes into the pull request description as one sentence for
  the owner, not into another round.
- A review reads the check run GitHub already made on the head commit; it runs the gate itself
  only when that commit has none.
- Independent tasks run in parallel and share one machine: a subagent takes its own port and its
  own database rather than the defaults, and drops them afterwards (run-app skill).
- Every subagent that writes to the repository gets `isolation: "worktree"`. A worktree has no
  `.env` (run-app skill); scratch files go under the session scratchpad in the agent's own
  directory.
- The main checkout never holds a delegated branch, `--ignore-other-worktrees` included: its
  working tree then reads as uncommitted changes that revert the agent's commits.
- Search with `Grep`, `Glob` and `Read`. The shell is for commands that change something — never
  `ls`, `cat`, `head`, `tail`, `wc`, `find` or `grep`.
- What runs together runs in one call: `add`, `commit` and `push` for one unit.
- A delegated task pushes every unit as it finishes it.
- A delegation with no worktree write for twenty minutes is dead: `TaskStop`, remove its
  worktree and local branch, delegate again from what reached the remote.
- Do the work directly where spawning costs more: one file to read, a one-line edit.
