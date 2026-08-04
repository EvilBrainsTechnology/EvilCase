#!/usr/bin/env pwsh
<#
    Enforces .claude/instruction-limits.json: fails naming every file over the per-file limit,
    and the sum over the total limit, each with how far over it is. In GitHub Actions it also
    writes a step-summary table, against the pull request's base branch or, outside one, the
    previous commit; counting that baseline can never fail the check.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Test-Path 'variable:PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) {
    Write-Output '::error::not inside a git repository'
    exit 1
}
Set-Location -LiteralPath $root

$limits = Get-Content -LiteralPath '.claude/instruction-limits.json' -Raw | ConvertFrom-Json
$perFileLimit = $limits.maxLinesPerFile
$totalLimit = $limits.maxLinesTotal

# `**/` stands for any number of directories, `*` for anything inside one segment.
$patterns = @($limits.globs | ForEach-Object {
        '^' + (([regex]::Escape($_) -replace '\\\*\\\*/', '(?:[^/]+/)*') -replace '\\\*', '[^/]*') + '$'
    })

function Test-Instruction([string] $Path) {
    if ($Path -match '(^|/)(\.git|bin|obj|node_modules)/') { return $false }
    foreach ($pattern in $patterns) {
        if ($Path -match $pattern) { return $true }
    }
    return $false
}

function Measure-WorkingTree {
    # Tracked files plus new ones git would accept, so a rule file written but not yet committed
    # counts, and .git, bin and obj are never walked.
    $paths = git -c core.quotePath=false ls-files --cached --others --exclude-standard
    $counted = [ordered] @{}
    foreach ($path in $paths | Sort-Object -Unique) {
        if ((Test-Instruction $path) -and (Test-Path -LiteralPath $path -PathType Leaf)) {
            $counted[$path] = @(Get-Content -LiteralPath $path).Count
        }
    }
    return [pscustomobject] @{ Counts = $counted }
}

function Measure-Baseline {
    if ($env:GITHUB_BASE_REF) {
        $ref = "origin/$($env:GITHUB_BASE_REF)"
        $label = $env:GITHUB_BASE_REF
    }
    else {
        $ref = 'HEAD^'
        $label = 'previous commit'
    }

    $paths = git -c core.quotePath=false ls-tree -r --name-only $ref 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }

    $counted = [ordered] @{}
    foreach ($path in $paths) {
        if (Test-Instruction $path) {
            $content = git show "${ref}:${path}" 2>$null
            if ($LASTEXITCODE -eq 0) { $counted[$path] = @($content).Count }
        }
    }
    return [pscustomobject] @{ Counts = $counted; Label = $label }
}

$counts = (Measure-WorkingTree).Counts
if ($counts.Count -eq 0) {
    Write-Output '::error::no instruction files matched the configured globs'
    exit 1
}

$total = ($counts.Values | Measure-Object -Sum).Sum
$failures = @(
    $counts.GetEnumerator() | Where-Object { $_.Value -gt $perFileLimit } | ForEach-Object {
        "$($_.Key): $($_.Value) lines, $($_.Value - $perFileLimit) over the per-file limit of $perFileLimit"
    }
)
if ($total -gt $totalLimit) {
    $failures += "instruction files in total: $total lines, $($total - $totalLimit) over the total limit of $totalLimit"
}

$base = Measure-Baseline
$delta = ''
if ($base) {
    $baseTotal = ($base.Counts.Values | Measure-Object -Sum).Sum
    if ($total -ne $baseTotal) { $delta = " ($('{0:+#;-#;0}' -f ($total - $baseTotal)) vs $($base.Label))" }
}

Write-Output "$($counts.Count) instruction files, $total/$totalLimit lines$delta, per-file limit $perFileLimit"
foreach ($failure in $failures) {
    Write-Output "::error::$failure"
}

if ($env:GITHUB_STEP_SUMMARY) {
    $rows = @("### AI instructions: $total/$totalLimit lines$delta", '')
    $rows += if ($base) { "| File | Lines / $perFileLimit | Δ |", '| --- | ---: | ---: |' }
    else { "| File | Lines / $perFileLimit |", '| --- | ---: |' }

    foreach ($entry in $counts.GetEnumerator() | Sort-Object -Property Value -Descending) {
        $over = if ($entry.Value -gt $perFileLimit) { " — **$($entry.Value - $perFileLimit) over**" } else { '' }
        if ($base) {
            $diff = $entry.Value - $(if ($base.Counts.Contains($entry.Key)) { $base.Counts[$entry.Key] } else { 0 })
            $rows += "| ``$($entry.Key)`` | $($entry.Value)$over | $(if ($diff) { '{0:+#;-#;0}' -f $diff }) |"
        }
        else {
            $rows += "| ``$($entry.Key)`` | $($entry.Value)$over |"
        }
    }

    if ($base) {
        foreach ($path in $base.Counts.Keys | Where-Object { -not $counts.Contains($_) } | Sort-Object) {
            $rows += "| ``$path`` | removed | $('{0:+#;-#;0}' -f (-$base.Counts[$path])) |"
        }
    }

    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $rows
}

exit $(if ($failures) { 1 } else { 0 })
