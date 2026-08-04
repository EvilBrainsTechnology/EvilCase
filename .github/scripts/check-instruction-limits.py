#!/usr/bin/env python3
"""Enforces .claude/instruction-limits.json: fails naming every file over the per-file limit,
and the sum over the total limit, each with how far over it is. In GitHub Actions it also
reports the counts as a step-summary table."""
import glob
import json
import os
import sys

with open(".claude/instruction-limits.json", encoding="utf-8") as config:
    limits = json.load(config)

files = sorted({path for pattern in limits["globs"] for path in glob.glob(pattern, recursive=True)})
if not files:
    print("::error::no instruction files matched the configured globs — run from the repository root")
    sys.exit(1)

per_file_limit = limits["maxLinesPerFile"]
total_limit = limits["maxLinesTotal"]

counts = []
for path in files:
    with open(path, encoding="utf-8") as file:
        counts.append((path, sum(1 for _ in file)))
total = sum(lines for _, lines in counts)

failures = [
    f"{path}: {lines} lines, {lines - per_file_limit} over the per-file limit of {per_file_limit}"
    for path, lines in counts
    if lines > per_file_limit
]
if total > total_limit:
    failures.append(f"instruction files in total: {total} lines, {total - total_limit} over the total limit of {total_limit}")

report = f"{len(files)} instruction files, {total}/{total_limit} lines, per-file limit {per_file_limit}"
print(report)
for failure in failures:
    print(f"::error::{failure}")

summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
if summary_path:
    with open(summary_path, "a", encoding="utf-8") as summary:
        summary.write(f"### AI instructions: {total}/{total_limit} lines\n\n")
        summary.write(f"| File | Lines / {per_file_limit} |\n| --- | ---: |\n")
        for path, lines in sorted(counts, key=lambda entry: -entry[1]):
            over = f" — **{lines - per_file_limit} over**" if lines > per_file_limit else ""
            summary.write(f"| `{path}` | {lines}{over} |\n")

sys.exit(1 if failures else 0)
