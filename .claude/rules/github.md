# GitHub

- Never push to `master`. All work goes through a pull request from a branch.
- Commit during the work: every logical unit that stands on its own is its own commit.
- A pull request carries one topic and stays small enough to review on a phone; when it grows,
  split it rather than let it run.
- Before a pull request exists: `dotnet r ci` green from `src/`, new tests covering what
  changed, visual proof of every changed screen (`.claude/skills/product-loop/visual-proof.md`),
  documentation updated in the same commit. A red gate is fixed, never worked around.
- A pull request opens as a draft and leaves draft only once the code review in
  `.claude/rules/agents.md` has run and its findings are worked in. Clearing the flag is the
  second `mcp__github__*` exception beside the merge — REST ignores `draft`, GraphQL is blocked.
- Subscribe (`subscribe_pr_activity`) to every pull request you open.
- A pull request is never a workbench: verify with tools, not with trial edits in it.
- Every git and GitHub interaction — commits, pull requests, comments, reviews, issues — is
  authored as `claude[bot]`: GitHub writes go through `curl` with `$GH_TOKEN` (endpoints in
  `.claude/skills/product-loop/github-api.md`), never through the `mcp__github__*` tools,
  which write as the owner. The merge is the one exception: this environment refuses a
  `$GH_TOKEN` merge into a protected branch, so it goes through
  `mcp__github__merge_pull_request` and is recorded under the owner's account.
- A pull request's title and description always match its current diff; update them with every
  change to its content.
- No attribution footer and no session link in a GitHub write: the `claude[bot]` author says it.
- The loop tends every open pull request: work review comments in, reply to them, rebase onto
  the target branch on a conflict, keep CI green.
- The owner is whoever `CODEOWNERS` names.
- The agent merges; the owner only approves. Dependent pull requests merge in dependency
  order: after each merge, rebase what conflicts and wait for green CI before the next.
- Merge only a pull request GitHub reports mergeable (`mergeable_state: clean`): the
  repository's configured merge requirements — reviews, checks, conflicts — are the gate.
  Squash-merge.
- Merge only into `master`: a pull request targeting another branch is a stacked layer and
  merges only after GitHub retargets it to `master`.
- Never approve your own pull request and never bypass the merge requirements: no auto-merge,
  no admin merge, no force push to `master`, no branch-protection change.
- The repository is public. No real case content, names, file marks or personal data anywhere:
  code, tests, docs, issues, pull requests, commit messages. Real case folders on the owner's
  disk are read-only reference.
