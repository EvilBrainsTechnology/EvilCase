# Backlog bootstrap

A round that finds no open `loop` issue at all derives the next backlog from
`docs/product/vision.md` before taking work. The vision's milestone table is the source;
`docs/sdr/README.md` maps each milestone to its governing SDRs.

- The GitHub milestones are `M1`–`M7` after the vision's table; the bootstrap creates the
  missing ones.
- Slice issues cover the lowest milestone whose deliverables the application does not yet
  have — one milestone per bootstrap, never all at once: a decision inside one milestone can
  still change the SDRs behind the next. Deliverables are checked against the code, never
  against closed issues.
- One issue is one slice: a pull request from database to UI that leaves the app usable,
  small enough to review on a phone. The split follows the milestone's deliverables and its
  SDRs; where an SDR names the slices, that split wins — SDR-006 names the four of M2.
- Each issue fills `.github/ISSUE_TEMPLATE/slice.md`, carries its milestone and the `loop`
  label, and names its governing SDRs in the body.
- Where an SDR orders slices, the later issue carries `Blocked by #`: in M2 the schema reset
  blocks numbering, the file storage core and the sample seed, and the seed also waits on
  those two.
- Labels come from `.claude/rules/github.md` and the issue templates; the bootstrap creates
  a missing one and never re-colours an existing one.
