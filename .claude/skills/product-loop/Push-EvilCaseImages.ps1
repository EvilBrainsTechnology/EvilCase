#!/usr/bin/env pwsh
<#
    Puts a pull request's screenshots on the docs/images orphan branch under pull-request/<number>/,
    and prints the finished markdown image block on stdout, one line per pushed image sorted by file
    name, ready to paste into the body's Screenshots section. The commit sha goes to stderr.

        ./.claude/skills/product-loop/Push-EvilCaseImages.ps1 -PullRequest <number> -Path /tmp/shots/<issue>

    -PullRequest is the number GitHub handed out and -Path the directory screenshots.mjs wrote;
    every *.png directly in it goes up under its own name. -Remote, -Branch and -Attempts have
    defaults, and -Message overrides the commit message.

    Plumbing throughout: nothing is checked out, and the checkout's own index and working tree are
    untouched, so a run costs a worktree nothing. A path the branch already holds is replaced
    silently — a body pinned to the earlier commit goes on showing what it showed, because a raw
    URL resolves against the commit it names.

    A rejected push means another round pushed first, and the wording says when. Already
    advertised: `! [rejected] … (fetch first)` for a tip this checkout has no object for,
    `(non-fast-forward)` when it has one — worktrees of a checkout share its object store. Landed
    while this push was in flight: `cannot lock ref … is at … but expected …`. All three re-fetch
    and re-parent the same files on the new tip, up to -Attempts times: git's hint to `git pull`
    is wrong here, and the `--force` behind it is what the branch's ruleset refuses.

    A -Branch the remote does not have is refused, never created: the ruleset keeps docs/images from
    being deleted, so a name that is not there is a typo, and pushing to it would put the body's
    URLs on a branch nothing protects. -Create writes the parentless commit a genuinely new branch
    starts from, and is refused in turn when the remote already has the branch — that is the same
    typo one flag further on.

    Fails, without retrying, on: a -Path holding no *.png, a -PullRequest below 1, a directory that
    is not an EvilCase checkout, and a push rejected for anything but the branch having moved.
#>
param(
    [int] $PullRequest,
    [string] $Path,
    [string] $Remote = 'origin',
    [string] $Branch = 'docs/images',
    [int] $Attempts = 5,
    [string] $Message,
    [switch] $Create
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

# The body's URLs need <owner>/<repo>, not the remote's name; both URL forms git supports point at
# github.com or this fails rather than guess.
$remoteUrl = (& git -C $root remote get-url $Remote).Trim()
if ($remoteUrl -match '^(?:https://github\.com/|git@github\.com:)(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?/?$') {
    $ownerRepo = "$($Matches.owner)/$($Matches.repo)"
}
else {
    throw "$Remote ($remoteUrl) is not a GitHub remote"
}

# Missing credentials are a failure, never a question: git asks on /dev/tty, which a redirected
# stdin does not close, and a tool call with no one at the keyboard hangs on it.
$env:GIT_TERMINAL_PROMPT = '0'

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

# A scratch index, so the checkout's own is never read or written. The tip comes from ls-remote and
# the fetch writes no FETCH_HEAD: both files are one per checkout, shared by every worktree of it.
$indexFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "evilcase-images-$PID.index")
$env:GIT_INDEX_FILE = $indexFile
try {
    for ($attempt = 1; ; $attempt++) {
        $listed = Invoke-GitChecked @('ls-remote', $Remote, "refs/heads/$Branch")
        $parent = if ($listed) { ($listed -split '\s+', 2)[0] } else { $null }
        if (-not $parent -and -not $Create) {
            throw "$Remote has no $Branch — a typo, or a branch to start with -Create"
        }
        # First attempt only: a rival creating the branch during a retry is the race the loop is for.
        if ($parent -and $Create -and $attempt -eq 1) {
            throw "$Remote already has $Branch — -Create starts one it does not have"
        }
        # Listed before fetched, never after: a tip that lands in between is fetched with the one
        # named here among its ancestors, and read-tree has an object either way.
        if ($parent) { Invoke-GitChecked @('fetch', '--no-write-fetch-head', $Remote, $Branch) | Out-Null }

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
            [Console]::Error.WriteLine($sha)
            foreach ($image in $images) {
                $basename = [System.IO.Path]::GetFileNameWithoutExtension($image.Name)
                Write-Output "![$basename](https://raw.githubusercontent.com/$ownerRepo/$sha/pull-request/$PullRequest/$basename.png)"
            }
            return
        }
        # The branch moved; the header has the three wordings. Anything else — the ruleset declining
        # a force among it — is not a race and is never retried.
        if ($push.Error -notmatch 'fetch first|non-fast-forward|cannot lock ref') {
            throw "git push $sha to $Branch failed: $($push.Error)"
        }
        if ($attempt -ge $Attempts) { throw "$Branch moved under all $Attempts attempts: $($push.Error)" }
        [Console]::Error.WriteLine("$Branch moved, attempt $attempt of $Attempts rejected; re-parenting")
        Start-Sleep -Seconds ([math]::Min(8, [math]::Pow(2, $attempt - 1)))
    }
}
finally {
    Remove-Item -LiteralPath $indexFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath 'env:GIT_INDEX_FILE' -ErrorAction SilentlyContinue
}
