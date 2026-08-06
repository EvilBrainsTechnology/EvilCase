# Instructions

- A rule says what to do and stops: not why it exists, not where it is enforced, not what it is
  better than.
- Instructions state invariants. No state, no progress, no history, no changelog.
- A rule lives in one file. Other files point at it.
- The loop never changes `.claude/**`. A missing or wrong rule becomes an issue for the owner.
- Outside the loop, change a rule only when future work would otherwise be wrong or ambiguous.
- An instruction file carries commands, never scripts. A block with control flow, failure
  handling or state to clean up is a pwsh script beside the instruction that calls it, with
  `Set-StrictMode`, `$ErrorActionPreference` and a header stating parameters and failures.
- Detail needed only occasionally goes to `docs/**` or a README and is referenced from the rule.
- Shorten elsewhere to fit a limit in `.claude/instruction-limits.json`; never raise one.
- No `@path` imports.
