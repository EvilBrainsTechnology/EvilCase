# GitHub

- Never push to `master`. Work goes through a pull request from a branch.
- A comment waits for an answer when its author is the owner, `vdolek`. Every other comment —
  the agent's own or a bot's — does not.
- Commit every logical unit that stands on its own, `add` and `commit` in one call; push
  every commit as it is made.
- A change in behaviour carries a test. Documentation changes in the same commit as the code.
- A pull request is one topic, opened ready for review. The body fills
  `.github/pull_request_template.md`. Title and description match the diff.
- An issue fills its matching template in `.github/ISSUE_TEMPLATE/`; the front matter is its
  title and labels. An agent's bug issue also carries `loop`.
- The full gate is CI on GitHub; locally runs only the coder's one check.
- A pull request that changes behaviour goes to the owner only reviewed: by the slice's reviewer
  agent, otherwise `/code-review high`, a rework on its own diff `/code-review medium`; plus
  `/security-review` where authentication or file storage change. Findings are fixed first; a
  `.github/code_review_template.md` comment records it.
- An agent's pull request carries exactly one state label — `agent-in-progress` (an agent works on
  it), `agent-done` (waiting for the owner), `waiting-for-agent` (waiting for an agent) — and
  setting one removes the rest. Every session switches to `agent-in-progress` before the work — on
  the issue until the pull request exists, on the pull request from its opening — and to
  `agent-done` when it finishes. `ci-failed` sits beside the state as a flag: a red CI run adds
  it, a green one removes it. A close strips the state labels and the flag; a workflow does it.
- Never merge and never resolve a review conversation. The owner does both.
- The repository is public. No real case content, names, file marks or personal data anywhere.
  Real case folders on the owner's disk are read-only reference.
