# Visual proof

Screenshot every changed screen at 1440×900 and 390×844 — the two sides of the `lg` breakpoint —
signed in as the seeded administrator, with the app started per the run-app skill. Everything in
a screenshot is synthetic data.

- `screenshots.mjs` next to this file takes them: one browser, one sign-in per width, every
  screen in one pass, and a non-zero exit when a page threw — a component that fails to render
  draws an empty card, not an error, so a screenshot alone would look fine.

  ```bash
  PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers /opt/node22/bin/node \
    .claude/skills/product-loop/screenshots.mjs /tmp/shots/192 targets.json
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
`pull-request/<number>/<screen>-<width>.png`. The branch is append-only and never force-pushed,
so its commits stay reachable however the feature branch is rebased or deleted.

Embed by raw URL pinned to the commit, never by branch name — a branch-name URL renders whatever
the path holds today, and a pull request has to keep showing what was reviewed:

```
https://raw.githubusercontent.com/EvilBrainsTechnology/EvilCase/<sha>/pull-request/<number>/<screen>-<width>.png
```

## The order

The number the images are filed under does not exist until the pull request does, so the body is
written twice.

1. Push the branch and open the pull request as a draft, body without images.
2. Take the screenshots into a directory outside the checkout.
3. Commit them onto `doc/images` and push. Plumbing, so nothing is checked out and the
   worktree keeps its own working tree and index:

   ```bash
   git fetch origin doc/images
   export GIT_INDEX_FILE=$(mktemp -u)          # scratch index; the checkout's own is untouched
   git read-tree origin/doc/images
   git update-index --add --cacheinfo \
     "100644,$(git hash-object -w /tmp/shots/192/case-list-1440.png),pull-request/192/case-list-1440.png"
   sha=$(git commit-tree "$(git write-tree)" -p origin/doc/images -m "Images for #192")
   git push origin "$sha:refs/heads/doc/images"
   unset GIT_INDEX_FILE
   ```

4. `PATCH` the body with the images, each pinned to `$sha`.
5. Read the body back and check every URL.

A round that supersedes a screen pushes the new file beside the old one and re-points the body;
nothing on `doc/images` is overwritten or deleted.

- Check a raw URL with `curl -o /dev/null -w '%{http_code}'` and **no** `Authorization` header —
  `raw.githubusercontent.com` answers `404` to `$GH_TOKEN` whatever the file.
- GitHub sometimes writes a URL back wrapped in a backtick, which renders as literal text. Read
  the body back after every write; where it happened, embed as `<img src="…" alt="…">` — editing
  the markdown again adds another backtick.
