# Writing

Applies to every text produced: documentation, AI instructions, code comments, commit messages,
pull requests, comments, issues, chat.

- Say it in the fewest words that still hold.
- Write plain declarative sentences. No metaphors, no aphorisms, no rhetorical structure.
- State what holds, not why. Give a reason only where the text misleads without it.
- No filler: no recaps, no lead-in sentences, no narrating a diff in words.
- Report a verification by its result, not its protocol.
- Never write about mutation-testing a test, about how many rounds something took, or about
  what was removed and why.
- No tables of evidence, no pasted SQL, no pasted tool output.
- Comment code in one or two lines, only where the code cannot say it. Rationale goes in the
  commit message.
- An SDD states a product rule or a product limit, never how the code implements it.
- Everything committed and every GitHub write is English. UI strings, `docs/product/vision.md`
  and `docs/sdd/**` are Czech. Chat follows the user's language; the loop's round report is Czech.
