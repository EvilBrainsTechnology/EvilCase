# Visual proof

Only a pull request that changes a screen carries screenshots. One width, 1440×900, signed in as
the seeded administrator, with the app started per the run-app skill. All data is synthetic.

- `.claude/skills/product-loop/screenshots.mjs` takes them and exits non-zero when a page threw.
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

The images never enter the slice's diff. They live on `docs/images`, an orphan branch, under
`pull-request/<number>/`, filed in this order:

1. Take the screenshots into `/tmp/shots/<issue>`. A non-zero exit ends it — fix the screen.
2. Open the pull request, body without images.
3. `./.claude/skills/product-loop/Push-EvilCaseImages.ps1 -PullRequest <n> -Path /tmp/shots/<issue>`
   pushes them and prints the body's Screenshots block on stdout, one markdown image line per
   pushed file, pinned to the commit it just made.
4. Paste that block verbatim under a `## Screenshots` heading, the last section before
   `Closes #`. Nothing else in that section: no `<img>`, no bare URL, no caption line, no bullet,
   no indentation.
5. Read the body back and fetch each URL. A URL that does not answer 200, or a line GitHub did
   not render as an image, means the body is written again from a file with
   `gh pr edit --body-file`; never left as it is.
