# Instructions

How the AI instruction files themselves change.

- Instructions describe invariants: no state, no progress, no history, no changelog, and no
  scripts. Commands may be fenced; a block with control flow, failure handling or state to clean
  up is a script beside the instruction that calls it — pwsh unless the runtime says otherwise,
  with `Set-StrictMode`, `$ErrorActionPreference` and a header stating parameters and failures.
- Change instructions during ordinary work, and only when future work would otherwise be wrong
  or ambiguous. Information that does not change the agent's behaviour stays out.
- Prefer rewording or replacing an existing rule; a new paragraph is the last resort. A rule
  is stated in one file only; another file points at it rather than restating it.
- An instruction change follows `.claude/rules/writing.md` like any other text.
- Cross-cutting rules live in `.claude/rules/`, path-scoped where they cover one area; a new
  area gets a rule file there. Implementation detail lives in a README next to the code and
  changes in the same commit.
- Information needed only occasionally belongs in `docs/**` or a README, referenced from the
  rules; those files have no length limit.
- The length limits in `.claude/instruction-limits.json` are permanent and CI-enforced. When a
  change would exceed one, shorten elsewhere. The agent may lower a limit, never raise one; a
  raise is the owner's decision, asked for as a decision issue.
- No `@path` imports in instruction files — imported lines dodge the limits.
