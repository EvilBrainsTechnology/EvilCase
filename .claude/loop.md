# Default loop for this repository

Run one iteration of the EvilCase product loop. Follow `.claude/commands/loop-step.md` exactly.

Order every round: apply answered decision issues first, then pick unblocked work, then ask about
every open product question in that work, then build and ship at most one pull request.

If every open issue is blocked on a decision, do nothing this round except re-surface the open
questions with their issue links.
