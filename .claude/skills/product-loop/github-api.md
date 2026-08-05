# GitHub REST reference

A row below is what the script beside this file takes; its header has the parameters and failures.

```bash
./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 <path> [-Method …] [-Json … | -JsonFile …] [-MarkdownFile …] [-Select …]
```

| Need | Call |
| --- | --- |
| Open issues | `issues?state=open` — **includes pull requests**; drop every element with a `pull_request` key |
| Issues by label | `issues?state=open&labels=needs-decision` |
| The loop's backlog, with priorities | `issues?state=open&labels=loop` — every element carries `issue_field_values`; the entry whose `issue_field_name` is `Priority` holds `single_select_option.name` (`Urgent`, `High`, `Medium`, `Low`). Plain REST returns it, no preview header |
| Open pull requests | `pulls?state=open` |
| One pull request, with `mergeable_state` | `pulls/{n} -Select number,mergeable_state` |
| Merge a pull request | `pulls/{n}/merge -Method PUT -Json '{"merge_method":"squash"}'` |
| Conversation comments | `issues/{n}/comments` |
| Inline review comments | `pulls/{n}/comments` |
| Reviews | `pulls/{n}/reviews` |
| Reply inside a review thread | `pulls/{n}/comments/{comment_id}/replies -MarkdownFile <file>` |
| Comment on an issue or pull request | `issues/{n}/comments -MarkdownFile <file>` |
| Create an issue | `issues -Json '{"title":"…","labels":["…"],"milestone":<id>}' -MarkdownFile <file>` |
| Edit an issue body, labels or state | `issues/{n} -Method PATCH -MarkdownFile <file>` / `-Json '{"state":"closed","state_reason":"completed"}'` |
| Add or remove one label | `issues/{n}/labels -Json '{"labels":["blocked"]}'` / `issues/{n}/labels/blocked -Method DELETE` |
| Create a pull request | `pulls -Json '{"title":"…","head":"loop/12-slug","base":"master","draft":true}' -MarkdownFile <file>` |
| Edit a pull request title or body | `pulls/{n} -Method PATCH -Json '{"title":"…"}' -MarkdownFile <file>` |
| Mark it ready for review | `mcp__github__update_pull_request` `{"draft":false}` — `PATCH` answers `200` and ignores `draft`, and this session's GraphQL serves only pinned review operations |
| CI state of a branch head | `commits/{sha}/check-runs` |
| Labels and milestones | `labels`, `milestones`, each also `-Json '{"name":"…"}'` to create |

## Stacks

| Call | What it does |
| --- | --- |
| `stacks` | The repository's stacks, newest first |
| `stacks/{number}` | One stack |
| `stacks -Json '{"pull_requests":[bottom,…,top]}'` | Creates one; at least two numbers, each base matching the previous head |
| `stacks/{number}/add -Json '{"pull_requests":[…]}'` | Appends to the top — the delta only, never the whole list |
| `stacks/{number}/unstack -Method POST` | **Dissolves the stack**, no body, no confirmation — never call it as a probe |

A pull request carries its membership as a `stack` object (`number`, `size`, `position`) — check
there, not by listing stacks. A stack keeps its merged and closed members; that is behaviour,
not a defect to clean up.

Merging a stacked pull request answers `403 Merging stacked PRs via this endpoint is not
supported`, and the asynchronous endpoint it names is not served here. `unstack` first, then
merge the bottom and rebase what was above it onto `master`.
