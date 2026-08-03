---
name: product-loop
description: Run one round of the EvilCase product loop — apply answered decisions, clear open pull requests, ship thin vertical slices, report. Use for every round of /loop, and whenever the loop's schedule, its decision issues, its GitHub access or its stacked pull requests are in question.
---

# EvilCase product loop

Move EvilCase toward `docs/product/vision.md`, one reviewable slice at a time. Never ask for permission to continue.

A round fires once an hour, so it is not one unit of work: it runs until nothing is tractable and then reports everything it did. Sections 1 to 7 are a cycle, not a list — clear everything §2 finds, take a slice, and when it is open come back to §3 while the round has room.

Read at the start of every round, before anything else: `docs/product/vision.md`, the root `CLAUDE.md`, and the open work (§1 and §2 below).

## GitHub without `gh`

**`gh` is not installed in this container and `github.com` is blocked by the egress policy.** `api.github.com` is reachable, `$GH_TOKEN` is in the environment, and `git` works against `origin`. Everything below is a `curl`; a round must never depend on a tool it might not have, including `mcp__github__*` — a Routine carries no connector grants unless the session that created it held them, so a fired round can wake with no MCP tools at all.

```bash
GH=https://api.github.com/repos/EvilBrainsTechnology/EvilCase
curl -s -H "Authorization: Bearer $GH_TOKEN" -H "Accept: application/vnd.github+json" "$GH/issues?state=open&per_page=100"
```

| Need | Call |
| --- | --- |
| Open issues | `GET $GH/issues?state=open&per_page=100` — **includes pull requests**; drop every element that has a `pull_request` key |
| Issues by label | `GET $GH/issues?state=open&labels=needs-decision` |
| Open pull requests | `GET $GH/pulls?state=open&per_page=100` |
| One pull request, with `mergeable_state` | `GET $GH/pulls/{n}` |
| Conversation comments on an issue or pull request | `GET $GH/issues/{n}/comments?per_page=100` |
| Inline review comments | `GET $GH/pulls/{n}/comments?per_page=100` |
| Reviews | `GET $GH/pulls/{n}/reviews?per_page=100` |
| Reply inside a review thread | `POST $GH/pulls/{n}/comments/{comment_id}/replies` `{"body":"…"}` |
| Comment on an issue or pull request | `POST $GH/issues/{n}/comments` `{"body":"…"}` |
| Create an issue | `POST $GH/issues` `{"title":"…","body":"…","labels":["…"],"milestone":<id>}` |
| Edit an issue body, labels or state | `PATCH $GH/issues/{n}` `{"body":"…"}` / `{"state":"closed","state_reason":"completed"}` |
| Add or remove one label | `POST $GH/issues/{n}/labels` `{"labels":["blocked"]}` / `DELETE $GH/issues/{n}/labels/blocked` |
| Create a pull request | `POST $GH/pulls` `{"title":"…","head":"loop/12-slug","base":"master","body":"…"}` |
| CI state of a branch head | `GET $GH/commits/{sha}/check-runs` |
| Labels and milestones | `GET $GH/labels`, `POST $GH/labels`, `GET $GH/milestones`, `POST $GH/milestones` |

**Identity is ambiguous, and `GET /user` is the wrong way to resolve it.** That endpoint answers `vdolek` for `$GH_TOKEN`, but every pull request, issue and comment written with the same token is attributed to `claude[bot]`. Measured, not assumed. So: identify the loop's own writing by the author of the written object — `user.login == "claude[bot]"` — and never by what `/user` says.

## 0. Work runs in subagents, not in the main thread

Everything the session does to this repository fills its context with build logs, diffs and API output until it compacts, and a compaction loses exactly the detail a later round needs. **The main thread delegates the work and keeps only the report.** That covers a round, and also answering review comments, taking an issue, and an investigation the owner asked for.

Delegating is not free, so the exception is size rather than kind: reading one file, checking one pull request's state, a one-line edit the owner just asked for. If spawning costs more than doing, do it.

The session is what stays — `subscribe_pr_activity` subscriptions belong to it, and so does the conversation the owner reads. That is why work is delegated rather than moved to a fresh session.

### A task is delegated whole

A subagent finishes what it was given: writes the code, runs `dotnet r ci`, commits in logical units, pushes the branch, opens the pull request, subscribes to it, replies in the review threads it answered, files the issues its findings deserve. It does not hand a finished branch back for someone else to publish. A task split at the last step costs the main thread the whole context it was delegated to avoid, and the half that comes back is the half that needs the detail.

Three things stay with the main thread, because a subagent cannot do them:

- **Talking to the owner.** A subagent has no one to ask, so a question it hits becomes a decision issue and the round carries on.
- **The schedule.** `list_triggers` and `create_trigger` are not reachable from a subagent.
- **Merging**, when the owner has asked for it by name — and never otherwise.

### Independent work runs in parallel, each in its own worktree

Review comments on three pull requests are three tasks that do not touch each other, and so are two unrelated issues. Spawn them together rather than one after another.

**Every subagent that writes to the repository gets `isolation: "worktree"`.** They otherwise share one checkout, and a `git checkout` in one destroys what another is holding — the failure is silent and looks like work that was never done. A read-only subagent does not need one. Two tasks that would edit the same file are not independent and do not go out together whatever the isolation says.

The main thread relays what came back; a subagent's report is not shown to the owner.

### A pull request is not a workbench

A comment, a description or a branch that the owner reads is a deliverable, not scratch space. Verify with the tools — `curl`, `git`, a build — never by writing a trial into a pull request and looking at what happened.

### The subagent starts blank, so the repository is the only memory

A subagent knows what is in the repository and on GitHub and nothing else. **Anything that has to survive the round is written there before the round ends** — never left in the chat, and never in the head of whichever agent noticed it:

- A finding that needs doing → an issue.
- The state of a pull request that its diff does not show — why it was closed, what it waits on, what was tried and abandoned → a comment on that pull request.
- A rule the loop has to follow → this file, or the `CLAUDE.md` that owns the area.

A report in the chat is for the owner to read, not a place to keep state. If the next round would have to be told something, it is not written down yet.

## 1. Apply answered decisions

Read the open `needs-decision` issues and the comments of each. **An answer is a comment whose author is not `claude[bot]`**, even if it is one word or picks no listed option. The loop's own comments are distinguishable, so it may comment on an open decision issue — to add context it missed, or to narrow the question. It still never answers one itself, and never re-asks a question that is already open.

For each answered decision: state what was chosen and what follows from it in a `## Decision` comment, label it `decided`, close it, and remove `blocked` from every issue that references it. If the decision changes the vision, `docs/product/vision.md` is updated in the next pull request, in the same commit as the code it governs; a decision that governs no code updates it on its own.

Unanswered decisions stay open.

## 2. Get what is open merged, before opening anything new

For each open pull request the loop authored, read the reviews and both kinds of comment, and find everything from the owner that has not been answered.

**Not "what is new since the last round" — what is unanswered.** A round that filters by timestamp misses anything written while an earlier round was still running, and from then on the comment is always older than the cutoff, so every later round misses it too. The age of a comment proves nothing either: a rebase rewrites every commit date on the branch, so *arrived after the last commit* stops meaning anything the moment the branch is rebased.

The test is the thread, not the clock: a review thread with no reply from `claude[bot]` is outstanding, however old it looks. Read the bodies. A count of comments is not a check, and treating one as a check is how #86 waited eleven hours.

**This is the round's first job and usually its whole job.** The measure of the loop is what reaches `master`, not how many branches it has open. An unmerged pull request holds up every slice above it, goes stale against a moving base, and costs the owner more the longer it waits — so a round that answers three review threads and rebases two branches has done more than one that opens a fourth pull request.

Every comment gets a response in the same round it is found. Nothing is deferred.

- A requested change → check the branch out, fix it, meet the whole definition of done again, push, and reply in the thread saying what changed and in which commit.
- A question → answer in the thread, no code.
- A product decision in disguise → open a decision issue, link it from the thread, leave the pull request open.

Then clear what stands between the rest and a merge, in this order: a red CI run, a merge conflict or a stale base (rebase it), a description that no longer matches its diff or whose screenshot URLs an earlier rebase has orphaned.

**A pull request the loop opened needs an explicit approval; one the owner opened does not.** Only the owner clears branch protection on `master`, so a green, answered, current pull request from the loop still sits at `mergeable_state: blocked`. That is the protection rule, not a defect — say so in the report rather than calling the branch unready.

Take new work only when every open pull request is green, current and answered.

## 3. Pick the work

Take the single highest-value open issue that is neither `blocked` nor `needs-decision`, preferring the lowest open milestone. Honour the focus argument if one was given.

**Between two candidates, take the one that lands on `master` on its own.** A slice that needs an unmerged branch adds a layer to something already waiting. Depth is only worth it when the work genuinely cannot exist otherwise — see *Stacked pull requests* below. If every remaining candidate would deepen a stack, say so in the report rather than deepening it by default.

- Backlog empty → derive the next thin vertical slice from the vision, open an issue for it, take it.
- Every open issue blocked → do not idle and do not guess. Post one chat message re-surfacing the open questions with their issue links, and stop the round.

Once a slice is open, come back here for the next one — unless the queue rule in *Standing rules* says the round is done opening things.

## 4. Ask before building — generously

The owner wants to be consulted often. Before writing code, list every product, domain or UX branch in this slice that has more than one reasonable answer, and ask about each one. Technical choices already governed by a `CLAUDE.md` — naming, layering, test structure, EF details — you make yourself, silently, without asking.

For each question, do both:

**Open a decision issue.** Title `[DECISION] <the question, short>`, label `needs-decision`, same milestone as the blocked issue, body:

```
## Context
Two or three sentences. What the slice is, why this branch exists.

## Options
### A — <name>  (recommended)
What it means. What it costs. What it makes impossible later.
### B — <name>
...

## Recommendation
A, because ...

## Blocks
#<issue>
```

**Ask the same question in the chat**, under ten lines, as a numbered choice with a recommendation, so it is answerable from a phone with one word.

Then label the dependent issue `blocked`, link the decision, and carry on with whatever is not blocked by it — a smaller part of the slice, or the next issue. A question never stops the loop.

## 5. Build one thin vertical slice

One pull request goes from database to UI and leaves the app usable. Small enough to review on a phone. Branch `loop/<issue>-<slug>`, and commit in logical units as the work proceeds.

Off `master` when the slice needs nothing that is still open. When it builds on an unmerged branch, branch off that one instead and target it.

The `CLAUDE.md` files are binding, without exception — the API client generator, the controller conventions, the analyzers at error severity, `internal sealed` behind interfaces, no `Async` suffix, the responsive rules, English in the repository and Czech in the UI strings.

Authentication already ships and is default-deny. Every new endpoint is authenticated and every new page sits inside `MainLayout`, therefore protected. Adding `[AllowAnonymous]` anywhere, or placing a page outside `MainLayout`, is a decision issue, never a silent choice. Every new aggregate root carries its owner from its first migration.

## 6. Definition of done

All four hold before the pull request exists:

- `dotnet r ci` green, run from `src/`.
- New tests covering what the slice adds, not only that it builds.
- Visual proof, as described below.
- Documentation updated in the same commit: the `CLAUDE.md` that owns the area for a cross-cutting rule, the README next to the code for implementation detail.

A red gate is fixed, never worked around. Analyzers are not suppressed to pass. If the slice cannot meet the gate, shrink the slice.

### Visual proof

Start PostgreSQL and the `evilcase` preview server as `.claude/skills/run-app/SKILL.md` describes, sign in as the seeded administrator, and screenshot every changed screen at 1440×900 and at 390×844, the two sides of the `lg` breakpoint.

There is no way to upload an image to GitHub outside the web interface, so the screenshots reach the pull request as committed files. **`docs/screenshots/` does not exist yet — the first slice that needs it creates it.**

- Save them as `docs/screenshots/<issue>/<screen>-<width>.png` and commit them with the slice.
- Embed them in the pull request body by raw URL pinned to that commit, which resolves before the branch is merged and after it is deleted: `https://raw.githubusercontent.com/EvilBrainsTechnology/EvilCase/<sha>/docs/screenshots/...`
- A slice that replaces a screen deletes the screenshots it supersedes in the same pull request, so the directory stays the current state of the application rather than its history.
- **A rebase orphans the pinned commit and every one of those URLs starts answering `404`.** Re-point them to the new head in the same step as the force-push, never later: a broken image shows up only when somebody opens the pull request.
- **Check a raw URL with `curl -o /dev/null -w '%{http_code}'` and no `Authorization` header.** `$GH_TOKEN` is an app token and `raw.githubusercontent.com` answers `404` to it whatever the file, so a check that sends it condemns every screenshot in the repository at once. That the URL resolves is the whole question; how GitHub renders the image is not, and is never worth a test comment in somebody's pull request.
- **A write can come back with the URL wrapped in a backtick** — ``![alt](`url`)``, which renders as literal text. Read the response body back after every write, and where that happened embed it as `<img src="url" alt="...">` instead. Editing the markdown again adds another backtick. #102 carries one such image.

Everything in the screenshots is synthetic, by the standing rule below.

## 7. Pull request

`POST $GH/pulls`: TL;DR on the first line, then what changed, the screenshots, and `Closes #<issue>`. Against `master`, or against the branch this one was built on. Then `subscribe_pr_activity` on it, so its comments and CI arrive in the session rather than waiting for a round.

If it is the second pull request of a chain, link the chain as a stack in the same step.

Then stop. The root `CLAUDE.md` forbids an agent to merge on its own initiative and to push to `master`; it lets an agent merge what the owner asked for by name, and **a round is the loop's own initiative, so that exception never applies inside one**. Not its own pull request, not after a green CI run, not because the round would otherwise look unfinished. The loop is the one most likely to be tempted, because it runs unattended and merging is the only thing standing between it and the next slice. It waits instead.

## 8. Report

One short chat message covering the whole round: everything that shipped with its pull request link, every pull request updated in answer to review feedback or rebased onto a moving base and what changed in it, what now waits on the owner with decision links, what comes next. A round that did several things reports several — length follows the work, but each line stays one line.

**The report is a report, never a question.** It ends by saying what the next round will take, and the loop takes it. Anything the owner has to answer is a decision issue, linked from the report and never a question in the chat that the loop then waits on. The owner reads the report to know what happened, not to unblock anything.

**Times are Prague time**, in the report and in every other message to the owner. The container runs UTC, so convert — `TZ=Europe/Prague date`.

The round is over when the report is written. **The turn is over when the next round is scheduled** — see *The schedule* below, and name its cadence in the report.

## The schedule

The loop runs unattended, and the way it fails is by stopping without saying so. Its clock is a **recurring schedule**, never a one-shot wake-up: a one-shot has to be re-armed at the end of every turn, and *any* message from the owner in between is a turn. One such turn that ends without re-arming ends the loop silently. That is how the loop died the first time.

- **The schedule is a Routine, hourly.** `create_trigger` bound to the session, and nothing else. It is stored server-side, so it is the only clock here that survives anything. Two of its properties must not be fought: the minimum interval is one hour, and an hourly expression is re-anchored to the minute it was created in — asking for `0 * * * *` yields `N * * * *`. A cadence finer than hourly cannot be had, so do not promise one.
- **`CronCreate` does not work in Claude Code on the web.** Its jobs live in the session's memory and this environment resumes the session between turns, which wipes them. Measured: three jobs created across one evening, none survived to its next fire, zero rounds ran. Never use it as the loop's only clock.
- **A session that starts while the loop is meant to be running arms it.** `list_triggers` first; if no Routine exists for the loop, create one. `create_trigger` needs a permission the agent must not grant itself — `.claude/settings.json` allows it, and a denial there is a question for the owner rather than something to work around.
- **Every turn ends by confirming the schedule is still there** — including a turn that had nothing to do with the loop, and above all one that interrupted a round. `list_triggers` is the check; an empty answer while the loop is meant to be running means recreate it before the turn ends.
- **State the cadence in the report.** A loop that has quietly stopped should be visible in the chat, not only in the absence of anything happening.
- **Watch the pull requests rather than poll them.** `subscribe_pr_activity` delivers comments, reviews and CI transitions into the session as `<github-webhook-activity>`, so a review comment is answered when it is written instead of at the next round. Subscribe to every pull request the loop opens, in the step that opens it, and re-subscribe to the open ones at the start of a session — a subscription belongs to the session and does not outlive it. Unsubscribe when the pull request merges or closes. It covers pull requests and nothing else: an answer on a decision issue is still found by the next round.

The loop ends when the owner says so, or by the one genuine stop in §3 — nothing buildable, said once. Both are stated out loud. A loop that stops any other way is a bug.

## Stacked pull requests

A slice that needs something not yet on `master` branches off the branch carrying it, and its pull request targets that branch. Two or more such pull requests are linked as a **stack** on GitHub, so the chain is one object with an order.

**A stack is a cost, not an achievement.** It exists so that work can continue across a merge the agent is not the one to decide on — not so that more of it can be started. Everything in a stack is unreviewed, unmerged and rebasing itself every time the floor moves. So: prefer a slice that lands on `master` on its own, take a layer only when the work genuinely cannot exist without an unmerged one, and shorten an existing chain from the bottom rather than extend it from the top. Getting what is already open into a state the owner can merge outranks starting anything new.

- **The base branch is the whole mechanism.** Each pull request's base is the head of the one below it, bottom to top, the bottom on `master`. A diff then shows only what its own layer adds.
- **Link the chain as soon as it is a chain.** A stack needs at least two pull requests; a slice that stands on its own opens an ordinary pull request against `master` and nothing more.
- `gh stack` is the tool on a workstation and is unavailable here, so use the REST API with `$GH_TOKEN`:

  | Call | What it does |
  | --- | --- |
  | `GET $GH/stacks` | The repository's stacks, newest first |
  | `GET $GH/stacks/{number}` | One stack |
  | `POST $GH/stacks` `{"pull_requests":[bottom,…,top]}` | Creates one; at least two numbers, and each base must match the previous head |
  | `POST $GH/stacks/{number}/add` `{"pull_requests":[…]}` | Appends to the top — the delta only, never the whole list |
  | `POST $GH/stacks/{number}/unstack` | **Dissolves the stack.** No body, no confirmation, and it answers `204` to a probe as readily as to an intention. Never call it to find out whether it exists. |

  A pull request carries its membership as a `stack` object (`number`, `size`, `position`), which is how to check a chain is linked without listing every stack.

- **A merge moves the floor.** When the bottom of a stack merges, GitHub retargets the one above it onto the merged base and the rest need rebasing onto the new `master`. That is work for whoever owns the branches, not a reason to touch the merge.
- **An agent does not decide a stack is ready**, and merging the bottom of one is still merging.

## Standing rules

- Real case folders on the owner's disk are read-only reference. Never write there, never copy a real document into the repository. Fixtures are synthetic files mimicking the naming convention.
- The repository is public. No real case content, names, file marks or personal data anywhere — code, tests, docs, issues, pull requests, commit messages.
- On a usage limit, wait for the reset and resume at the same point. Never trade the definition of done for tokens.
- Prefer reversible steps. A destructive migration, a dependency change, a change to authentication or the security headers, a rewrite of something that already works: ask first, as a decision issue.
- **A round is bounded by the work, not by a count.** Clearing what is already open — comments, red checks, stale bases — is unbounded: do all of it. Then take slices one after another for as long as the round has room.
- **Each pull request stays one thin slice.** Doing more per round means several small pull requests, never one big one; they are small because they are reviewed on a phone.
- **Do not grow a queue the owner cannot get through.** With six or more pull requests already waiting, spend the round on those and open nothing new. Say so in the report instead.
- **The loop never waits for the owner.** It opens the decision issue and carries on with whatever is not blocked by it. It does not pause for approval to commit, to open a pull request, to close its own pull request, or to pick the next slice — those are the loop's to make, and the owner reverses any of them at leisure. The only genuine stop is the one in §3.
