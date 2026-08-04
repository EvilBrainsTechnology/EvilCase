#!/usr/bin/env python3
"""Enforces .claude/instruction-limits.json: fails naming every file over the per-file limit,
and the sum over the total limit, each with how far over it is. In GitHub Actions it reports
the counts as a step-summary table; on a pull request also the difference against the base
branch (counted from the fetched base tree with the current config)."""
import glob
import json
import os
import subprocess
import sys
import tempfile

with open(".claude/instruction-limits.json", encoding="utf-8") as config:
    limits = json.load(config)


def count(root):
    found = sorted({path for pattern in limits["globs"] for path in glob.glob(pattern, recursive=True, root_dir=root)})
    counted = {}
    for path in found:
        with open(os.path.join(root, path), encoding="utf-8") as file:
            counted[path] = sum(1 for _ in file)
    return counted


def count_base():
    base_ref = os.environ.get("GITHUB_BASE_REF")
    if not base_ref:
        return None, None
    archive = subprocess.run(["git", "archive", f"origin/{base_ref}"], capture_output=True)
    if archive.returncode != 0:
        return None, None
    with tempfile.TemporaryDirectory() as tree:
        subprocess.run(["tar", "-x", "-C", tree], input=archive.stdout, check=True)
        return count(tree), base_ref


counts = count(".")
if not counts:
    print("::error::no instruction files matched the configured globs — run from the repository root")
    sys.exit(1)

per_file_limit = limits["maxLinesPerFile"]
total_limit = limits["maxLinesTotal"]
total = sum(counts.values())

failures = [
    f"{path}: {lines} lines, {lines - per_file_limit} over the per-file limit of {per_file_limit}"
    for path, lines in counts.items()
    if lines > per_file_limit
]
if total > total_limit:
    failures.append(f"instruction files in total: {total} lines, {total - total_limit} over the total limit of {total_limit}")

base, base_ref = count_base()
delta = f" ({total - sum(base.values()):+d} vs {base_ref})" if base is not None else ""
print(f"{len(counts)} instruction files, {total}/{total_limit} lines{delta}, per-file limit {per_file_limit}")
for failure in failures:
    print(f"::error::{failure}")

summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
if summary_path:
    with open(summary_path, "a", encoding="utf-8") as summary:
        summary.write(f"### AI instructions: {total}/{total_limit} lines{delta}\n\n")
        if base is None:
            summary.write(f"| File | Lines / {per_file_limit} |\n| --- | ---: |\n")
        else:
            summary.write(f"| File | Lines / {per_file_limit} | Δ |\n| --- | ---: | ---: |\n")
        for path, lines in sorted(counts.items(), key=lambda entry: -entry[1]):
            over = f" — **{lines - per_file_limit} over**" if lines > per_file_limit else ""
            if base is None:
                summary.write(f"| `{path}` | {lines}{over} |\n")
            else:
                diff = lines - base.get(path, 0)
                summary.write(f"| `{path}` | {lines}{over} | {f'{diff:+d}' if diff else ''} |\n")
        if base is not None:
            for path in sorted(set(base) - set(counts)):
                summary.write(f"| `{path}` | removed | {-base[path]:+d} |\n")

sys.exit(1 if failures else 0)
