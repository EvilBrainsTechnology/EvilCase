# Visual proof

Screenshot every changed screen at 1440×900 and 390×844 — the two sides of the `lg` breakpoint —
signed in as the seeded administrator, with the app started per the run-app skill. Everything in
a screenshot is synthetic data.

- Playwright is at `/opt/node22/lib/node_modules/playwright`, its browsers at `/opt/pw-browsers`
  (`PLAYWRIGHT_BROWSERS_PATH` points there). Drive it from an `.mjs` script importing the
  absolute path — a bare `'playwright'` resolves from neither the repository nor a worktree.
  Never run `playwright install`; the download is blocked.
- Save as `docs/screenshots/<issue>/<screen>-<width>.png` and commit with the slice. A slice
  that replaces a screen deletes the screenshots it supersedes, so the directory stays the
  current state of the application.
- Embed in the pull request body by raw URL pinned to the commit:
  `https://raw.githubusercontent.com/EvilBrainsTechnology/EvilCase/<sha>/docs/screenshots/…`
- A rebase orphans the pinned commit and every URL starts answering `404`: re-point them in the
  same step as the force-push, never later.
- Check a raw URL with `curl -o /dev/null -w '%{http_code}'` and **no** `Authorization` header —
  `raw.githubusercontent.com` answers `404` to `$GH_TOKEN` whatever the file.
- GitHub sometimes writes a URL back wrapped in a backtick, which renders as literal text. Read
  the body back after every write; where it happened, embed as `<img src="…" alt="…">` — editing
  the markdown again adds another backtick.
