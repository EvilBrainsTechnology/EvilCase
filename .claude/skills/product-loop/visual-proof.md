# Visual proof

Only a pull request that changes a screen carries screenshots. One width, 1440×900, signed in as
the seeded administrator, with the app started per the run-app skill. All data is synthetic.

- `screenshots.mjs` next to this file takes them and exits non-zero when a page threw.
  `EVILCASE_WIDTHS=1440,390` adds the mobile side where a change is about responsive layout.

  ```bash
  PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers /opt/node22/bin/node \
    .claude/skills/product-loop/screenshots.mjs /tmp/shots/<issue> targets.json
  ```

  `targets.json` is a list of `{ name, path, file, wait?, steps?, fullPage? }`; a step is
  `{ click?, fill?: [selector, value], wait? }`. `EVILCASE_URL`, `EVILCASE_EMAIL` and
  `EVILCASE_PASSWORD` override the defaults. Extend the script rather than write a second one.
- Playwright is at `/opt/node22/lib/node_modules/playwright`, its browsers at `/opt/pw-browsers`.
  Never run `playwright install`.

The images never enter the slice's diff. They live on `doc/images`, an orphan branch, under
`pull-request/<number>/`, filed in this order:

1. Take the screenshots into `/tmp/shots/<issue>`. A non-zero exit ends it — fix the screen.
2. Open the pull request, body without images.
3. `./.claude/skills/product-loop/Push-EvilCaseImages.ps1 -PullRequest <n> -Path /tmp/shots/<issue>`
   pushes them and prints the commit sha.
4. Edit the body to embed each as
   `https://raw.githubusercontent.com/EvilBrainsTechnology/EvilCase/<sha>/pull-request/<n>/<name>.png`,
   pinned to that sha, never to a branch name.
5. Read the body back. Where GitHub wrapped a URL in a backtick, embed as `<img src="…">`.
