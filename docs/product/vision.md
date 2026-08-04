# Product vision

Source of truth for what EvilCase is being built into. The product loop reads this file at the
start of every round, and the bootstrap derives labels, milestones and the backlog from it.
Roadmap and open questions live in GitHub Issues, not here.

## What EvilCase is

A case-file system for administrative and legal proceedings. A case grows over time: acts are
added, documents arrive and are sent, sub-proceedings branch off. The product keeps that whole
tree in one place and answers, at any moment, what happened and what is in the file.

The current target is a **first usable base**: one person keeps a real case file by hand —
every case, act, document, party and note lives in the application instead of a folder tree.
Deliberately small; what the base leaves out is a non-goal below and comes later, step by step.

## Horizon

- Now: a single user, own case files, manual entry. Optimise for speed of working a real case.
- Later, possibly: multi-tenant SaaS for law firms. Sign-in, sessions and default-deny
  authorization already ship; every aggregate root carries its owner from its first migration
  (`.claude/rules/data.md`); nothing else of tenancy is built.

## Domain model

**Case** — root of a proceeding. Carries owner, status, tags, parties, file marks and comments.
A case nests under another case to any depth; a sub-case has the same shape. Deleting a case
deletes its sub-tree.

**Act** (*úkon*) — the unit of work inside a case: one submission, decision, notice or call. It
has a direction (outgoing/incoming), a title, one calendar date, the file number (*číslo
jednací*) of whoever issued it, an editable summary, and links to file assets. Acts have no
ordinal: every act list is ordered by the act date, with the record's creation time as the
fallback. The summary lives on the act and nowhere else — an attachment that came from another
act is read through the summary of that act.

**File asset** — a stored blob, content-addressed by hash and deduplicated per owner: the same
PDF attached in five sub-cases is one asset with five links. Each link carries a role —
`Source` (the .docx), `Final` (the .pdf), `Attachment`, `DeliveryReceipt`, `Envelope`, `Other`
— a display name and, where the asset originates from another act, a reference to that act.
Files upload and download in the browser. Removing a file from an act removes the link; bytes
with no remaining link go with it.

**Party** — an authority, an official or a person; flat, reused across cases; carries a
data-box id and a free-text address printed back as a block. Picked or created inline wherever
a case, act or mark names one, and managed in a standalone agenda that shows where each party
appears. A party can be deleted only while nothing references it.

**Case reference** — a file mark (*spisová značka*). A case has one internal mark plus N
external marks, each bound to the party that assigned it, because every authority in the chain
assigns its own.

**Comment** — a free note on a case or an act. The running log of the file.

**Status and tags** — status is a small closed set (`Active`, `WaitingOnAuthority`, `Closed`);
tags are free text.

Everything the user enters can be edited and deleted; a destructive operation confirms first.

## Sample data

`EvilBrains__EvilCase__Database__SeedSampleData` (default `false`) seeds the database at
startup, in any environment, only while it holds no case. The data is the pseudonymised
speeding case from `test-data/case-01-speeding.md`, whole: the sub-case lines, parties, marks,
acts with synthetic PDF bytes, comments — so every screen is built and verified against a real
file's depth, including one document shared by several sub-cases.

## Priorities

In order, from what hurts most when working a real case:

1. The whole case in one place — the tree, the acts, the documents — instead of a folder tree.
2. Entering a new act with its documents in under a minute.
3. Finding a case fast; search ignores diacritics.

## Milestones

| # | Milestone | Ships |
| --- | --- | --- |
| M1 | Act date and sample data | act model trimmed to one date and no ordinal, every act list ordered by date; the seed switch loading the speeding case |
| M2 | Case UI | case detail with the sub-case tree and comments; create, edit and delete; references, status and tags; diacritics-insensitive search |
| M3 | Acts and files | act list in the case detail; act page with summary, files and comments; add, edit and delete an act; upload and download |
| M4 | Parties | the standalone agenda; inline pick-or-create everywhere a party is named |

The base is done when a real case file can be kept by hand end to end.

## Non-goals for now

Deadlines, timeline, folder import (test data enters ad hoc or through the seed), text
extraction and fulltext, .docx templates and generated submissions, data-box (ISDS)
integration, e-mail intake, AI summaries, multi-tenancy, roles, invitations, billing, user
management beyond the seeded administrator. The model leaves room for each; none is built.

## Privacy

The repository is public. `.claude/rules/github.md` keeps real case content out of every write;
test fixtures are synthetic or pseudonymised (`test-data/README.md`). Real case folders on disk
are read-only reference material.
