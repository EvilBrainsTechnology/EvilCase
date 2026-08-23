# Instructions

- A rule says what to do and stops: not why it exists, not where it is enforced, not what it is
  better than.
- Instructions state invariants. No state, no progress, no history, no changelog.
- A rule lives in one file. Other files point at it.
- `.claude/**` and `docs/sdd/**` change only on the owner's explicit request; a hook blocks
  every other edit. Such a request first runs `touch .claude/allow-meta-edits` (untracked),
  edits, then deletes the flag. Code that falsifies an instruction or an SDD gets an issue
  for the owner, never the edit. Never write down what the code already shows.
- An instruction file carries commands, never scripts. A block with control flow, failure
  handling or state to clean up is a pwsh script beside the instruction that calls it, with
  `Set-StrictMode`, `$ErrorActionPreference` and a header stating parameters and failures.
- Detail needed only occasionally goes to `docs/**` or a README and is referenced from the rule.
- Shorten elsewhere to fit a limit in `.claude/instruction-limits.json`; never raise one.
- No `@path` imports.
