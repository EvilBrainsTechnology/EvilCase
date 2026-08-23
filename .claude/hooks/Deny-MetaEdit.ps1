#!/usr/bin/env pwsh
<#
    PreToolUse hook: denies Edit, MultiEdit, Write and NotebookEdit into .claude/**,
    docs/sdd/**, docs/product/vision.md and any CLAUDE.md unless the target checkout holds
    the flag file .claude/allow-meta-edits.
    Reads the hook JSON on stdin. Exit 2 denies the call with the reason on stderr; exit 0
    allows it. Fails open: unparsable input or a tool call with no file path allows the call.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
    $hook = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $path = $null
    foreach ($name in 'file_path', 'notebook_path') {
        $property = $hook.tool_input.PSObject.Properties[$name]
        if ($property -and $property.Value) { $path = [string] $property.Value; break }
    }
    if (-not $path) { exit 0 }

    $path = $path -replace '\\', '/'

    # A worktree under .claude/worktrees/ is its own checkout: only the part after the last
    # worktree segment decides, and the flag is looked for in that checkout.
    $prefix = ''
    if ($path -match '^(?<pre>.*/\.claude/worktrees/[^/]+/)(?<rest>.+)$') {
        $prefix = $Matches['pre']
        $path = $Matches['rest']
    }
    $protected = '(\.claude/|docs/sdd/|docs/product/vision\.md$|CLAUDE\.md$)'
    if ($path -notmatch "^(?<mid>.*?/)?$protected") { exit 0 }
    $mid = if ($Matches.ContainsKey('mid')) { $Matches['mid'] } else { '' }

    $checkout = "$prefix$mid"
    if (-not $checkout) { $checkout = '.' }
    if (Test-Path -LiteralPath (Join-Path $checkout '.claude/allow-meta-edits')) { exit 0 }

    [Console]::Error.WriteLine(
        'Edits under .claude/** and docs/sdd/** are blocked. If the owner explicitly requested ' +
        'this change, run `touch .claude/allow-meta-edits`, retry the edit, and delete the ' +
        'flag afterwards. Otherwise open an issue for the owner instead of editing.')
    exit 2
}
catch {
    exit 0
}
