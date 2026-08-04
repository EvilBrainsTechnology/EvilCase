# Writing

Applies to every text produced: documentation, READMEs, AI instructions, code comments, commit
messages, pull request titles and descriptions, pull request comments and replies, issue bodies,
chat messages.

- Brevity over completeness: a shorter text covering 90 % beats a long one covering 100 %.
- State what holds, not why. Add a reason only where the text misleads without it.
- No filler: no recaps of what was already said, no lead-in sentences, no sections for effect,
  no narrating a diff in words.
- Comment code only where the code cannot say it, in one or two lines; no `<remarks>` essays,
  no copied `<inheritdoc/>`. Rationale goes in the commit message or the pull request, never in
  code.
- Commit messages and pull request descriptions open with a one- or two-sentence TL;DR; the rest
  only where it adds information.
- A reply to a review comment says what changed, or why not. Nothing more.
- A chat response ends with a short TL;DR of what waits on the user — a pull request review, an
  open decision; when nothing does, it ends without one.
- Chat responses follow the language of the user's message. Everything committed and every
  GitHub write is English; user-facing UI strings are Czech.
