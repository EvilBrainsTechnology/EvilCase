---
name: bootstrap-backlog
description: One-shot creation of the GitHub milestones and slice issues from `docs/product/vision.md` and its SDDs. Run only on the owner's request; the product loop never bootstraps.
---

# Bootstrap the backlog

The vision's milestone table is the source; `docs/sdd/README.md` maps each milestone to its
governing SDDs. The run is idempotent: an existing milestone, label or open slice issue is
left alone; only what is missing is created.

- The GitHub milestones are `M1`–`M7` after the vision's table.
- Slice issues cover every milestone whose deliverables the application does not yet have.
  Deliverables are checked against the code, never against closed issues.
- One issue is one slice: a pull request from database to UI that leaves the app usable,
  small enough to review on a phone. The split follows the milestone's deliverables and its
  SDDs; where an SDD names the slices, that split wins — SDD-007 names the four of M2.
- Each issue fills `.github/ISSUE_TEMPLATE/slice.md`, carries its milestone and the `loop`
  label, and names its governing SDDs in the body. `Priority` stays unset; the milestone
  orders the backlog.
- A slice built on another slice's deliverable carries `Blocked by #` naming it: in M2 the
  schema reset blocks the other three and the sample seed also waits on numbering and the
  file storage core; every issue past M2 waits at least on the M2 sample seed; a slice whose
  UI lands on a page another slice delivers waits on that slice.
- Labels come from `.claude/rules/github.md` and the issue templates; a missing one is
  created, an existing one never re-coloured.
