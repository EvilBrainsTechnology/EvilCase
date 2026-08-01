# Default loop for this repository

Claude Code delivers this file as the instruction for every round of `/loop` started without a
prompt of its own. It is the whole brief for that round.

Run **one** iteration of the EvilCase product loop, following `.claude/commands/loop-step.md`
exactly — read it at the start of the round, it is the specification and this file is only the
entry point.

Order, every round:

1. Apply the decision issues the owner has answered since the last round.
2. Pick the highest-value open issue that is neither `blocked` nor `needs-decision`.
3. Open a decision issue for every product question in it, and never comment on one that is open —
   `gh` runs as the owner, so the loop would be answering itself.
4. Build and ship at most one pull request, against the definition of done in `loop-step.md`.

Then stop and report. One round is one iteration; the next round is the next wake-up, not a
continuation of this one.

If every open issue is blocked on a decision, do nothing this round except re-surface the open
questions with their issue links. If that is still true on the round after it, say so once and
schedule no further work until an answer arrives — an unattended loop with nothing to do should go
quiet rather than invent work.
