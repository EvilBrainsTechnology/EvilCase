#!/usr/bin/env python3
"""Enforces .claude/instruction-limits.json: fails naming every file over the per-file limit,
and the sum over the total limit, each with how far over it is."""
import glob
import json
import sys

with open(".claude/instruction-limits.json", encoding="utf-8") as config:
    limits = json.load(config)

files = sorted({path for pattern in limits["globs"] for path in glob.glob(pattern, recursive=True)})
per_file_limit = limits["maxLinesPerFile"]
total_limit = limits["maxLinesTotal"]

failures = []
total = 0
for path in files:
    with open(path, encoding="utf-8") as file:
        lines = sum(1 for _ in file)
    total += lines
    if lines > per_file_limit:
        failures.append(f"{path}: {lines} lines, {lines - per_file_limit} over the per-file limit of {per_file_limit}")

if total > total_limit:
    failures.append(f"instruction files in total: {total} lines, {total - total_limit} over the total limit of {total_limit}")

print(f"{len(files)} instruction files, {total}/{total_limit} lines, per-file limit {per_file_limit}")
for failure in failures:
    print(f"::error::{failure}")
sys.exit(1 if failures else 0)
