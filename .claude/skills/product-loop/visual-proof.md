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
`pull-request/<number>/<screen>-<width>.png`. A ruleset on `refs/heads/doc/images` refuses
deletion and non-fast-forward pushes, so what is pushed there stays reachable.

Embed by raw URL pinned to the commit, never by branch name — a branch-name URL renders whatever
the path holds today, and a pull request has to keep showing what was reviewed:

```
https://raw.githubusercontent.com/EvilBrainsTechnology/EvilCase/<sha>/pull-request/<number>/<screen>-<width>.png
```

A URL pinned to a commit outside `doc/images` — one of the feature branch — dies with the next
rebase: re-point it in the same step as the force-push, never later.

## The order

The screenshots come first: `screenshots.mjs` exiting non-zero is what stops a broken screen
becoming a pull request. Only filing and embedding them need the number, which does not exist
until the pull request does, so the body is written twice.

1. Take the screenshots into a directory outside the checkout. A non-zero exit ends it here —
   fix the screen, open nothing.
2. Push the branch and open the pull request as a draft, body without images.
3. Commit the files onto `doc/images` under the number it got and push. Plumbing, so nothing is
   checked out and the worktree keeps its own working tree and index:

   ```bash
   git fetch origin doc/images
   export GIT_INDEX_FILE=$(mktemp -u)          # scratch index; the checkout's own is untouched
   git read-tree origin/doc/images
   for f in /tmp/shots/192/*.png; do
     git update-index --add --cacheinfo \
       "100644,$(git hash-object -w "$f"),pull-request/192/$(basename "$f")"
   done
   sha=$(git commit-tree "$(git write-tree)" -p origin/doc/images -m "Images for #192")
   git push origin "$sha:refs/heads/doc/images"
   unset GIT_INDEX_FILE
   ```

   `rejected … non-fast-forward` means another round pushed first, and git's hint to `git pull`
   is wrong here — nothing of `doc/images` is checked out, and the `--force` that follows it is
   what the ruleset refuses. Run the block again from `git fetch`: it re-parents the same files
   on the new tip and keeps both rounds'.
4. `PATCH` the body with the images, each pinned to `$sha`, then check every URL with
   `curl -o /dev/null -w '%{http_code}'` and **no** `Authorization` header —
   `raw.githubusercontent.com` answers `404` to `$GH_TOKEN` whatever the file.
5. Read the body back. GitHub sometimes writes a URL back wrapped in a backtick, which renders
   as literal text; where it happened, embed as `<img src="…" alt="…">` — editing the markdown
   again adds another backtick.

`update-index` replaces a path it already holds, without a word, and `screenshots.mjs` names the
files from `targets.json` — so a second round of the same targets supersedes them at the same
path. A body pinned to the earlier commit goes on showing what it showed: a raw URL resolves
against the commit it names.

Should `doc/images` ever be gone — `fatal: couldn't find remote ref doc/images` — the block
above rebuilds it: drop the `fetch`, the `read-tree` and `-p`, and `commit-tree` writes the
parentless commit the branch starts from. The old pinned URLs do not come back with it.
