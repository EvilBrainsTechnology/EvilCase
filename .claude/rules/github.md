# GitHub

- Never push to `master`. All work goes through a pull request from a branch.
- Commit during the work: every logical unit that stands on its own is its own commit.
- Every git and GitHub interaction — commits, pull requests, comments, reviews, issues,
  merges — is authored as `claude[bot]`: GitHub writes go through `curl` with `$GH_TOKEN`,
  never through the `mcp__github__*` tools, which write as the owner.
- A pull request's title and description always match its current diff; update them with every
  change to its content.
- The loop tends every open pull request: work review comments in, reply to them, rebase onto
  the target branch on a conflict, keep CI green.
- The owner is whoever `CODEOWNERS` names.
- The agent merges; the owner only approves. Dependent pull requests merge in dependency
  order: after each merge, rebase what conflicts and wait for green CI before the next.
- Merge only a pull request GitHub reports mergeable (`mergeable_state: clean`): the
  repository's configured merge requirements — reviews, checks, conflicts — are the gate.
  Squash-merge, then delete the branch.
- Merge only into `master`: a pull request targeting another branch is a stacked layer and
  merges only after GitHub retargets it to `master`.
- Never approve your own pull request and never bypass the merge requirements: no auto-merge,
  no admin merge, no force push to `master`, no branch-protection change.
- The repository is public. No real case content, names, file marks or personal data anywhere:
  code, tests, docs, issues, pull requests, commit messages. Real case folders on the owner's
  disk are read-only reference.
