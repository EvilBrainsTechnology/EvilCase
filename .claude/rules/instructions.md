# Instructions

- Instructions state invariants. No state, no progress, no history, no changelog.
- A rule lives in one file. Other files point at it.
- The loop never changes `.claude/**`. A missing or wrong rule becomes an issue for the owner.
- Outside the loop, change a rule only when future work would otherwise be wrong or ambiguous.
- An instruction file carries commands, never scripts. A block with control flow, failure
  handling or state to clean up is a pwsh script beside the instruction that calls it, with
  `Set-StrictMode`, `$ErrorActionPreference` and a header stating parameters and failures.
- Detail needed only occasionally goes to `docs/**` or a README and is referenced from the rule.
- The limits in `.claude/instruction-limits.json` are CI-enforced. Shorten elsewhere to fit;
  never raise a limit.
- No `@path` imports.
