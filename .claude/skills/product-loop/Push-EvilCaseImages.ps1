#!/usr/bin/env pwsh
<#
    Puts a pull request's screenshots on the doc/images orphan branch under pull-request/<number>/,
    and prints the commit sha, and nothing else, on stdout — the body pins its raw URLs to it.

        ./.claude/skills/product-loop/Push-EvilCaseImages.ps1 -PullRequest <number> -Path /tmp/shots/<number>

    -PullRequest is the number GitHub handed out and -Path the directory screenshots.mjs wrote;
    every *.png directly in it goes up under its own name. -Remote, -Branch and -Attempts have
    defaults, and -Message overrides the commit message.

    Plumbing throughout: nothing is checked out, and the checkout's own index and working tree are
    untouched, so a run costs a worktree nothing. A path the branch already holds is replaced
    silently — a body pinned to the earlier commit goes on showing what it showed, because a raw
    URL resolves against the commit it names.

    A rejected push means another round pushed first — `rejected … non-fast-forward` when the new
    tip was already advertised, `cannot lock ref … is at … but expected …` when it landed while
    this push was in flight. Either way the script re-fetches and re-parents the same files on the
    new tip, up to -Attempts times: git's hint to `git pull` is wrong here, and the `--force`
    behind it is what the branch's ruleset refuses. A branch that is gone — `couldn't find remote
    ref` — is written as the parentless commit it starts from, and the URLs pinned to the old one
    do not come back with it.

    Fails, without retrying, on: a -Path holding no *.png, a -PullRequest below 1, a directory that
    is not an EvilCase checkout, and a push rejected for anything but the branch having moved.
#>
param(
    [int] $PullRequest,
    [string] $Path,
    [string] $Remote = 'origin',
    [string] $Branch = 'doc/images',
    [int] $Attempts = 5,
    [string] $Message
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Refused, never prompted for: a missing mandatory parameter reads stdin, and a tool call with no
# one at the keyboard hangs on it.
if ($PullRequest -lt 1) { throw '-PullRequest <number> names the pull request the images belong to' }
if (-not $Path) { throw '-Path <directory> is the directory screenshots.mjs wrote' }
if ($Attempts -lt 1) { throw '-Attempts is at least 1' }
if (-not $Message) { $Message = "Images for #$PullRequest" }

if (Test-Path 'variable:PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

# From the script's own location, never the caller's directory — the same reason Start-EvilCase.ps1
# does: run from another repository, the git root is that one.
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..' '..' '..')).Path
if (-not (Test-Path -LiteralPath (Join-Path $root 'src'))) { throw "$root is not an EvilCase checkout" }

$images = @(Get-ChildItem -LiteralPath $Path -File -Filter '*.png' | Sort-Object -Property Name)
if (-not $images) { throw "no *.png in $Path" }

# stderr is kept rather than shown: the push writes its rejection there, and which rejection it is
# decides between a retry and an error.
function Invoke-Git([string[]] $GitArguments) {
    $errorFile = [System.IO.Path]::GetTempFileName()
    try {
        $output = & git -C $root @GitArguments 2> $errorFile
        return [pscustomobject] @{
            ExitCode = $LASTEXITCODE
            Output = ($output -join [Environment]::NewLine).Trim()
            Error = ((Get-Content -LiteralPath $errorFile -Raw) ?? '')
        }
    }
    finally { Remove-Item -LiteralPath $errorFile -Force -ErrorAction SilentlyContinue }
}

function Invoke-GitChecked([string[]] $GitArguments) {
    $result = Invoke-Git $GitArguments
    if ($result.ExitCode -ne 0) { throw "git $($GitArguments -join ' ') failed: $($result.Error)" }
    return $result.Output
}

# A scratch index, so the checkout's own is never read or written. FETCH_HEAD rather than a
# remote-tracking ref: a fetch of one branch is not obliged to write one.
$indexFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "evilcase-images-$PID.index")
$env:GIT_INDEX_FILE = $indexFile
try {
    for ($attempt = 1; ; $attempt++) {
        $fetch = Invoke-Git @('fetch', $Remote, $Branch)
        $parent = $null
        if ($fetch.ExitCode -eq 0) { $parent = Invoke-GitChecked @('rev-parse', 'FETCH_HEAD') }
        elseif ($fetch.Error -notmatch "couldn't find remote ref") {
            throw "git fetch $Remote $Branch failed: $($fetch.Error)"
        }

        Remove-Item -LiteralPath $indexFile -Force -ErrorAction SilentlyContinue
        if ($parent) { Invoke-GitChecked @('read-tree', $parent) | Out-Null }

        foreach ($image in $images) {
            $blob = Invoke-GitChecked @('hash-object', '-w', '--', $image.FullName)
            Invoke-GitChecked @('update-index', '--add', '--cacheinfo',
                "100644,$blob,pull-request/$PullRequest/$($image.Name)") | Out-Null
        }

        $commitArguments = @('commit-tree', (Invoke-GitChecked @('write-tree')))
        if ($parent) { $commitArguments += @('-p', $parent) }
        $sha = Invoke-GitChecked ($commitArguments + @('-m', $Message))

        $push = Invoke-Git @('push', $Remote, "${sha}:refs/heads/$Branch")
        if ($push.ExitCode -eq 0) {
            Write-Output $sha
            return
        }
        # The branch moved, and which wording says so depends on when: `non-fast-forward` when the
        # push already saw the new tip advertised, `cannot lock ref … is at … but expected …` when
        # it moved after the advertisement. Anything else — the ruleset declining a force among it —
        # is not a race and is never retried.
        if ($push.Error -notmatch 'non-fast-forward|fetch first|stale info|cannot lock ref') {
            throw "git push $sha to $Branch failed: $($push.Error)"
        }
        if ($attempt -ge $Attempts) { throw "$Branch moved under all $Attempts attempts: $($push.Error)" }
        # stderr: stdout carries the sha.
        [Console]::Error.WriteLine("$Branch moved, attempt $attempt of $Attempts rejected; re-parenting")
        Start-Sleep -Seconds ([math]::Min(8, [math]::Pow(2, $attempt - 1)))
    }
}
finally {
    Remove-Item -LiteralPath $indexFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath 'env:GIT_INDEX_FILE' -ErrorAction SilentlyContinue
}
