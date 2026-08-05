# doc/images

Images embedded in EvilCase pull request bodies. An orphan branch: no code, no history in common
with `master`, never merged anywhere.

- One directory per pull request: `pull-request/<number>/`.
- Append only. A file already pushed is never replaced and the branch is never force-pushed — the
  bodies link raw URLs pinned to a commit here, and a rewritten history turns every one of them
  into a `404`.
- The procedure that puts files here is `.claude/skills/product-loop/visual-proof.md` on `master`.
