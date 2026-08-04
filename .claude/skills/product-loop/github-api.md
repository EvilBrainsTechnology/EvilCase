# GitHub REST reference

```bash
GH=https://api.github.com/repos/EvilBrainsTechnology/EvilCase
curl -s -H "Authorization: Bearer $GH_TOKEN" -H "Accept: application/vnd.github+json" "$GH/…"
```

| Need | Call |
| --- | --- |
| Open issues | `GET $GH/issues?state=open&per_page=100` — **includes pull requests**; drop every element with a `pull_request` key |
| Issues by label | `GET $GH/issues?state=open&labels=needs-decision` |
| Open pull requests | `GET $GH/pulls?state=open&per_page=100` |
| One pull request, with `mergeable_state` | `GET $GH/pulls/{n}` |
| Merge a pull request | `PUT $GH/pulls/{n}/merge` `{"merge_method":"squash"}` |
| Conversation comments | `GET $GH/issues/{n}/comments?per_page=100` |
| Inline review comments | `GET $GH/pulls/{n}/comments?per_page=100` |
| Reviews | `GET $GH/pulls/{n}/reviews?per_page=100` |
| Reply inside a review thread | `POST $GH/pulls/{n}/comments/{comment_id}/replies` `{"body":"…"}` |
| Comment on an issue or pull request | `POST $GH/issues/{n}/comments` `{"body":"…"}` |
| Create an issue | `POST $GH/issues` `{"title":"…","body":"…","labels":["…"],"milestone":<id>}` |
| Edit an issue body, labels or state | `PATCH $GH/issues/{n}` `{"body":"…"}` / `{"state":"closed","state_reason":"completed"}` |
| Add or remove one label | `POST $GH/issues/{n}/labels` `{"labels":["blocked"]}` / `DELETE $GH/issues/{n}/labels/blocked` |
| Create a pull request | `POST $GH/pulls` `{"title":"…","head":"loop/12-slug","base":"master","body":"…"}` |
| Edit a pull request title or body | `PATCH $GH/pulls/{n}` `{"title":"…","body":"…"}` |
| CI state of a branch head | `GET $GH/commits/{sha}/check-runs` |
| Labels and milestones | `GET $GH/labels`, `POST $GH/labels`, `GET $GH/milestones`, `POST $GH/milestones` |

## Stacks

| Call | What it does |
| --- | --- |
| `GET $GH/stacks` | The repository's stacks, newest first |
| `GET $GH/stacks/{number}` | One stack |
| `POST $GH/stacks` `{"pull_requests":[bottom,…,top]}` | Creates one; at least two numbers, each base matching the previous head |
| `POST $GH/stacks/{number}/add` `{"pull_requests":[…]}` | Appends to the top — the delta only, never the whole list |
| `POST $GH/stacks/{number}/unstack` | **Dissolves the stack**, no body, no confirmation — never call it as a probe |

A pull request carries its membership as a `stack` object (`number`, `size`, `position`) — check
there, not by listing stacks. A stack keeps its merged and closed members; that is behaviour,
not a defect to clean up.
