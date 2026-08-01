# Product vision

Source of truth for what EvilCase is being built into. The autonomous product loop
(`.claude/commands/loop-step.md`) reads this file at the start of every iteration.
Roadmap and open questions live in GitHub Issues, not here.

## What EvilCase is

A case-file system for administrative and legal proceedings. A case grows over time: acts are
added, documents arrive and are sent, deadlines start running, sub-proceedings branch off. The
product keeps that whole tree in one place and answers three questions at any moment: what
happened, what is running, what has to be done next.

## Horizon

- Now: single user, own case files. Optimise for speed of working a real case.
- Later, possibly: multi-tenant SaaS for law firms.
- Sign-in already ships — seeded administrator, closed registration, refresh-token sessions,
  default-deny authorization (`AGENTS.md`, *Authentication*). What tenancy still needs is ownership
  on the aggregates and user management. Build the first as it goes; leave the second.
- Consequence: every aggregate root carries its owner from its first migration, there is no global
  mutable state, and no query assumes a single user. Roles, invitations and billing are not built.
- The column is therefore M0 work and every milestone after it, while M8 is the enforcement: until
  then a single user owns everything, and nothing filters by owner because nothing has to.

## Domain model

**Case** — root of a proceeding. Carries owner, status, tags, parties and file marks. A case may be
nested under another case to any depth; a nested case is a sub-case and has the same shape. In
imported folder trees a sub-folder is a sub-case.

**Act** (*úkon*) — the unit of work inside a case, and the thing the user thinks in. One act is
one submission, decision, notice or call. It has an ordinal within its case, a direction
(outgoing/incoming), a title, dates (drafted, sent, delivered, received), the file number
(*číslo jednací*) of whoever issued it, a summary, and links to file assets.

The summary lives here and nowhere else. It is not on the file asset: an asset is shared by every
link that points at it, while a summary is about what was said in one act. An attachment that came
from another act is read through the summary of that act, which its link already references.

**File asset** — a stored blob, content-addressed by hash. Never duplicated: the same PDF used as
an attachment in six sub-cases is one asset with six links. Each link carries a role — `Source`
(the .docx), `Final` (the .pdf), `Attachment`, `DeliveryReceipt` (*doručenka*), `Envelope`
(a data-box .zfo) — and, where the asset originates from another act, a reference to that act, so
an attachment reads as "the appellate decision of 15 March" rather than as an opaque file.

**Party** — an authority, an official or a person. Reused across cases; carries address and
data-box id. An act references the party that issued it and the party it is addressed to. A party
accumulates history across all cases.

**Case reference** — a file mark (*spisová značka*). A case has one internal mark plus N external
marks, each bound to the party that assigned it, because every authority in the chain assigns its
own.

**Comment** — a free note on a case or an act. The running log of the case.

**Deadline** — derived (a delivery date plus a statutory period) or entered by hand. Belongs to a
case or an act.

**Status and tags** — status is a small closed set (`Active`, `WaitingOnAuthority`, `Closed`);
tags are free text.

**Timeline** — a read model merging the acts, comments and deadlines of a case and all its
descendants into one chronological view.

## Folder naming convention (import source)

Existing case files live as folder trees on disk. The importer reads this convention:

| Pattern | Meaning |
| --- | --- |
| `NN - Title.ext` | act number `NN` of the enclosing case |
| `NNa - Attachment N - Title.ext` | attachment of act `NN` |
| same stem as `.docx` and `.pdf` | one act: source and final |
| sub-folder | sub-case |
| ` (uzavřeno)` suffix on a folder | the sub-case is closed |
| `.zfo` | data-box envelope of the neighbouring act |
| `99 - ...` | generated summaries, not acts |

The importer is a pure parser over the tree plus an execution step. It never writes to the source.

## Priorities

In order, from what hurts most when working a real case:

1. One timeline of the whole case including all sub-cases.
2. Deadlines: derived from delivery receipts, watched across every case.
3. Fulltext over document content, plus the act summary above it (manual now, AI later).
4. Generating a submission from a `.docx` template pre-filled from the case.

Both entry paths ship early: importing an existing folder tree, and creating cases and acts by hand.

## Milestones

| # | Milestone | Ships |
| --- | --- | --- |
| M0 | Domain core | case tree, party, references, act, file asset, comment — model, migrations, tests |
| M1 | Case UI | list with search, detail with sub-case tree, create/edit |
| M2 | Acts UI | act list, add act with files, act detail |
| M3 | Import | convention parser, dry-run preview, execution with dedupe |
| M4 | Timeline | merged read model and its UI |
| M5 | Deadlines | derivation from delivery, cross-case "what is due" |
| M6 | Search | text extraction, fulltext UI, the act summary editable by hand |
| M7 | Templates | template model, generated submission |
| M8 | Ownership and users | ownership enforced in every query and endpoint, user management, the tenancy seam |

Sign-in, sessions and default-deny authorization are already in place and are not a milestone.
Every screen and endpoint added from M0 on is authenticated.

## Non-goals for now

Data-box (ISDS) integration, e-mail intake, AI-generated summaries, multi-tenancy, roles,
invitations, billing. The model leaves room for each; none is built.

## Privacy

The repository is public to read. Real case documents, names, file marks and personal data never
enter it — not in code, tests, fixtures, docs, issues or pull requests. Test fixtures are
synthetic files that mimic the naming convention and nothing else. Real case folders on disk are
read-only reference material.
