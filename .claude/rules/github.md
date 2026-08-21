# GitHub

- Never push to `master`. Work goes through a pull request from a branch.
- A comment waits for an answer when its author is the owner, `vdolek`. Every other comment —
  the agent's own or a bot's — does not.
- Commit every logical unit that stands on its own, `add` and `commit` in one call; push once,
  at the end of the work.
- A change in behaviour carries a test. Documentation changes in the same commit as the code.
- A pull request is one topic, opened ready for review. The body fills
  `.github/pull_request_template.md`; its comments are the instructions and stay out of the
  body. Title and description match the diff.
- An issue fills its matching template in `.github/ISSUE_TEMPLATE/` where one fits. A
  template's front matter is the issue's title and labels. An agent's bug issue also carries
  `loop`.
- The gate is CI on GitHub. Nobody runs a local gate before a pull request.
- A pull request carries exactly one state label — `agent-in-progress` (an agent works on it),
  `agent-done` (waiting for the owner), `waiting-for-agent` (waiting for an agent) — and
  setting one removes the rest. Autonomous work — the loop and its workflows — switches to
  `agent-in-progress` before the work, not with the push, and to `agent-done` when it finishes.
  A session working under the owner's live instruction leaves the state labels alone.
  `ci-failed` sits beside the state as a flag: a red CI run adds it, a green one removes it.
  Closing an issue removes `agent-in-progress` and adds `agent-done`.
- Never merge and never resolve a review conversation. The owner does both.
- The repository is public. No real case content, names, file marks or personal data anywhere.
  Real case folders on the owner's disk are read-only reference.
