# GitHub REST reference

A row below is what the script beside this file takes; its header has the parameters and failures.

```bash
./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 <path> [-Method …] [-Json … | -JsonFile …]
    [-MarkdownFile …] [-Where …] [-Select …] [-Repository owner/name] [-Attempts 3]
```

`<path>` is relative to the repository; one starting with `/` reaches outside it, to
api.github.com (`/rate_limit`, `/repos/{owner}/{repo}`), and nowhere else — the token rides on
every call. The method is `GET`, and `POST` as soon as a body is given, so a row below carrying
`-Json`, `-JsonFile` or `-MarkdownFile` and no `-Method` is a `POST`. A GET is followed through
its pages at 100 per page, which is why no row names `per_page`. `-Select` prints one
tab-separated column per name, one line per element of an array; a name over an array maps, so
`labels.name` is a column of names rather than the whole label objects. `-Where '!pull_request'`
drops the elements carrying that key, before any column is taken.

| Need | Call |
| --- | --- |
| Open issues | `issues?state=open -Where '!pull_request'` — GitHub lists open pull requests among the issues, and this is what leaves them out |
| Issues by label | `issues?state=open&labels=needs-decision -Where '!pull_request'` |
| The loop's backlog, with priorities | `issues?state=open&labels=loop -Where '!pull_request' -Select number,issue_field_values.issue_field_name=Priority.single_select_option.name,milestone.number,labels.name` — the priority column is `Urgent`, `High`, `Medium`, `Low` or empty; skip the rows whose labels hold `blocked` or `needs-decision`. Plain REST returns the field, no preview header |
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
| CI state of a branch head | `commits/{sha}/check-runs -Select check_runs.name,check_runs.conclusion` — `{sha}` is `pulls/{n} -Select head.sha`; the endpoint wraps its array, so past 100 runs a second page joins as a second wrapper |
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
