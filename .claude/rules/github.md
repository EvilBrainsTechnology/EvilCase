# GitHub

- Never push to `master`. All work goes through a pull request from a branch.
- Commit during the work: every logical unit that stands on its own is its own commit.
- Every git and GitHub interaction — commits, pull requests, comments, reviews, issues,
  merges — is authored as `claude[bot]`.
- A pull request's title and description always match its current diff; update them with every
  change to its content.
- The loop tends every open pull request: work review comments in, reply to them, rebase onto
  the target branch on a conflict or a stale base, keep CI green.
- The owner is whoever `CODEOWNERS` names.
- The agent merges; the owner only approves. Merge order across dependent pull requests is the
  agent's: merge in dependency order, rebase the remaining branches after each merge, and wait
  for green CI before the next.
- Merge only a pull request with the owner's `APPROVED` review, green CI and no conflicts.
  Without the owner's approval merge nothing, without exception. Squash-merge, then delete the
  branch.
- Never approve your own pull request and never bypass the review gate: no auto-merge, no admin
  merge, no force push to `master`, no branch-protection change.
- The repository is public. No real case content, names, file marks or personal data anywhere:
  code, tests, docs, issues, pull requests, commit messages. Real case folders on the owner's
  disk are read-only reference.
