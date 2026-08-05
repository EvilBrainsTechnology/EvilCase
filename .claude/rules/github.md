# GitHub

- Never push to `master`. Work goes through a pull request from a branch.
- Commit every logical unit that stands on its own.
- One topic per pull request, small enough to review on a phone. Split it rather than let it grow.
- Read and write GitHub through `gh`. Where `gh` is missing, use `curl` with `$GH_TOKEN`.
- End every GitHub write with the footer `— 🤖 Claude Code` on its own last line. A comment
  without it is the owner's and is waiting for an answer.
- While working, run only what the change needs: `dotnet r build`, and tests filtered to the
  types you touched.
- Immediately before opening the pull request, run `dotnet r ci` from `src/` exactly once and get
  it green. Nobody runs it again — not the reviewer, not a later round.
- A change in behaviour carries a test. Documentation changes in the same commit as the code.
- Open the pull request ready for review, never as a draft.
- Body: one or two sentences of TL;DR, bullets of what changed, the assumption where one was
  made, the screenshot where a screen changed, `Closes #<n>`, the footer. At most 1500 characters.
- A reply to a review comment is at most three sentences: what changed, or why not.
- Title and description always match the current diff.
- Never merge. The owner merges. Rebase onto `master` on a conflict, keep CI green, and answer
  every review comment in the round that finds it.
- Subscribe (`subscribe_pr_activity`) to every pull request you open.
- Review another author's pull request only when it carries `request-code-review`.
- The repository is public. No real case content, names, file marks or personal data anywhere.
  Real case folders on the owner's disk are read-only reference.
