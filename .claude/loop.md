# Default loop for this repository

Run **one** round of the EvilCase product loop. `.claude/skills/product-loop/SKILL.md` is the
specification — read it at the start of the round; this file is only the entry point. The round runs
in a subagent with a clean context; the main thread spawns it, relays the report, confirms the
schedule.

Order, every round:

1. Apply the decision issues the owner has answered since the last round.
2. Answer every unanswered comment on an open pull request and clear what stands between it and a
   merge — a red check, a stale base, a description that no longer matches the diff. This outranks
   new work.
3. Pick the highest-value open issue that is neither `blocked` nor `needs-decision`, preferring one
   that lands on `master` on its own.
4. Open a decision issue for every product question in it, then carry on with what is not blocked.
5. Ship the slice against the definition of done — then back to 3 while the round has room.

The round ends when nothing is left that it can move, not when the first thing is finished; then
report all of it at once. Never merge, never push to `master`.
