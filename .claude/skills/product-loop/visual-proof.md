# Visual proof

Screenshot every changed screen at 1440×900 and 390×844 — the two sides of the `lg` breakpoint —
signed in as the seeded administrator, with the app started per the run-app skill. Everything in
a screenshot is synthetic data.

- `screenshots.mjs` next to this file takes them: one browser, one sign-in per width, every
  screen in one pass, and a non-zero exit when a page threw — a component that fails to render
  draws an empty card, not an error, so a screenshot alone would look fine.

  ```bash
  PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers /opt/node22/bin/node \
    .claude/skills/product-loop/screenshots.mjs docs/screenshots/153 targets.json
  ```

  `targets.json` is a list of `{ name, path, file, wait?, steps?, fullPage? }`; a step is
  `{ click?, fill?: [selector, value], wait? }`. `EVILCASE_URL`, `EVILCASE_EMAIL` and
  `EVILCASE_PASSWORD` override the defaults. Extend the script when a screen needs something it
  cannot express; do not write a second one.
- Playwright is at `/opt/node22/lib/node_modules/playwright`, its browsers at `/opt/pw-browsers`.
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
