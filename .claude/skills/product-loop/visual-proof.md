# Visual proof

Screenshot every changed screen at 1440×900 and 390×844 — the two sides of the `lg` breakpoint —
signed in as the seeded administrator, with the app started per the run-app skill. Everything in
a screenshot is synthetic data.

- `screenshots.mjs` next to this file takes them: one browser, one sign-in per width, every
  screen in one pass, and a non-zero exit when a page threw — a component that fails to render
  draws an empty card, not an error, so a screenshot alone would look fine.

  ```bash
  PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers /opt/node22/bin/node \
    .claude/skills/product-loop/screenshots.mjs /tmp/shots/<issue> targets.json
  ```

  `targets.json` is a list of `{ name, path, file, wait?, steps?, fullPage? }`; a step is
  `{ click?, fill?: [selector, value], wait? }`. `EVILCASE_URL`, `EVILCASE_EMAIL` and
  `EVILCASE_PASSWORD` override the defaults. Extend the script when a screen needs something it
  cannot express; do not write a second one.
- Playwright is at `/opt/node22/lib/node_modules/playwright`, its browsers at `/opt/pw-browsers`.
  Never run `playwright install`; the download is blocked.

## Where the images live

Never in the slice's diff — on `master` a screenshot has no reader. They live on `doc/images`,
an orphan branch sharing no history with `master`, one directory per pull request:
`pull-request/<number>/<screen>-<width>.png`. A ruleset on `refs/heads/doc/images` refuses
deletion and non-fast-forward pushes, so what is pushed there stays reachable.

Embed by raw URL pinned to the commit, never by branch name — a branch-name URL renders whatever
the path holds today, and a pull request has to keep showing what was reviewed:

```
https://raw.githubusercontent.com/EvilBrainsTechnology/EvilCase/<sha>/pull-request/<number>/<screen>-<width>.png
```

A URL pinned to a commit outside `doc/images` — one on the feature branch — dies with the next
rebase: re-point it in the same step as the force-push, never later.

## The order

The screenshots come first: `screenshots.mjs` exiting non-zero is what stops a broken screen
becoming a pull request. Only filing and embedding them need the number, which does not exist
until the pull request does, so the body is written twice.

1. Take the screenshots into `/tmp/shots/<issue>`, outside the checkout and named by the number
   that exists now. A non-zero exit ends it here — fix the screen, open nothing.
2. Push the branch and open the pull request as a draft, body without images.
3. Put the files on `doc/images` under the number it got, with `Push-EvilCaseImages.ps1` next to
   this file; it prints the commit sha the body pins to. Its header has the rest.

   ```bash
   sha=$(pwsh .claude/skills/product-loop/Push-EvilCaseImages.ps1 \
     -PullRequest <number> -Path /tmp/shots/<issue>)
   ```
4. `PATCH` the body with the images, each pinned to `$sha`, then check every URL with
   `curl -o /dev/null -w '%{http_code}'` and **no** `Authorization` header —
   `raw.githubusercontent.com` answers `404` to `$GH_TOKEN` whatever the file.
5. Read the body back. GitHub sometimes writes a URL back wrapped in a backtick, which renders
   as literal text; where it happened, embed as `<img src="…" alt="…">` — editing the markdown
   again adds another backtick.
