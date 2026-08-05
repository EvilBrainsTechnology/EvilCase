# Writing

Applies to every text produced: documentation, AI instructions, code comments, commit messages,
pull requests, comments, issues, chat.

- Say it in the fewest words that still hold. A short text covering 90 % beats a long one.
- Write plain declarative sentences. No metaphors, no aphorisms, no rhetorical structure.
- State what holds, not why. Give a reason only where the text misleads without it.
- No filler: no recaps, no lead-in sentences, no narrating a diff in words.
- Report a verification by its result, not its protocol. Green is green.
- Never write about mutation-testing a test, about how many rounds something took, or about
  what was removed and why.
- No tables of evidence, no pasted SQL, no pasted tool output.
- Limits: pull request description 1500 characters, comment three sentences, issue body 800
  characters.
- Comment code in one or two lines, only where the code cannot say it. Rationale goes in the
  commit message.
- Everything committed and every GitHub write is English. UI strings and
  `docs/product/vision.md` are Czech. Chat follows the user's language.
