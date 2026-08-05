#!/usr/bin/env pwsh
<#
    One GitHub REST call as `claude[bot]`, and the response on stdout. It exists because a
    worktree-isolated agent's shell refuses a command that expands $GH_TOKEN: the expansion lives
    here, and every call goes through it. The endpoints are in github-api.md beside it.

        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 pulls/12 -Select number,mergeable_state
        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 'issues?state=open' -Where '!pull_request' -Select number,title
        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 issues/12/comments -MarkdownFile /tmp/x/reply.md
        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 issues/12 -Method PATCH -Json '{"state":"closed"}'

    -Path is relative to the repository (`pulls/12`); one starting with / is relative to
    api.github.com and so reaches outside the repository (`/rate_limit`), and a full URL stands as
    it is — `https://api.github.com/…` alone, because the bearer token rides on it. -Method
    defaults to GET, or to POST when a body is given. -Repository is the `owner/name` a relative
    path hangs off, and -Attempts how many times a retryable failure is tried.

    The body is JSON, from -Json on the command line or -JsonFile for what does not fit on one.
    -MarkdownFile fills the `body` property from a file of plain markdown — a comment, a reply or a
    pull request description is written as markdown and never escaped by hand, backticks, $, quotes
    and newlines included. It combines with -Json or -JsonFile carrying the other properties, and
    is refused when those already name `body`.

    stdout is the response, verbatim. -Select prints named properties instead — one tab-separated
    column per name, one line per element of an array — so `-Select number` after opening a pull
    request is its number and nothing else, with no second call. A dotted path walks into nested
    objects. Against an array a number takes that element, `name=value` the first element carrying
    it — quote a value that holds a dot, `labels.name='v1.0'.color` — and any other name maps over
    every element, so `labels.name` is the array of label names.

    The data decides an empty column; the shape decides a failure. Empty is a JSON `null` on the way
    or at the end, an empty string, and a `name=value` no element answers — an unset field looks
    exactly like that, and the note naming the values that were there goes to stderr, once each.
    A path the shape does not have fails the whole call and prints no rows at all: an unknown
    property, an index past the end, and a `name=value` whose key no element carries.
    A column is one line whatever the data holds: an object or an array is compact JSON — `[]` and
    `[{…}]` included — a string's backslashes, newlines and tabs come out as \\, \n and \t, and a
    timestamp is the string the API sent.

    -Where keeps the elements one path resolves to something other than `null` on, `!path` those it
    does not, and is the one place a property the data lacks is an answer rather than a failure. It
    runs before -Select, so `issues?state=open -Where '!pull_request'` is the issues without the
    pull requests GitHub lists among them.

    A GET asks for 100 per page unless the path names its own `per_page`, and is followed through
    its `Link: rel="next"` pages, at most 20 of them; a 21st fails rather than walk a server that
    keeps advertising one. The pages are then the joined array, an array however many elements they
    hold between them. Every call gives up after 30 s, headers and body together — long enough for
    any page here, short enough that a stalled server fails the round instead of hanging it.

    A non-2xx puts the status and the response on stderr and fails. Two of them are retried instead,
    up to -Attempts times, waiting what `Retry-After` says or 2, 4, 8 … seconds: a 429 or a secondary
    rate limit whatever the method, both of which mean nothing was written, and a 5xx on GET alone,
    where a repeat cannot write twice. A missing $GH_TOKEN, and -Json together with -JsonFile, fail
    before the call.
#>
param(
    [Parameter(Position = 0)] [string] $Path,
    [string] $Method,
    [string] $Json,
    [string] $JsonFile,
    [string] $MarkdownFile,
    [string[]] $Select,
    [string] $Where,
    [string] $Repository = 'EvilBrainsTechnology/EvilCase',
    [int] $Attempts = 3
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Refused, never prompted for: a missing mandatory parameter reads stdin, and a tool call with no
# one at the keyboard hangs on it.
if (-not $Path) { throw '<path> names the endpoint, `pulls/12` or `/rate_limit` or a full URL' }
if ($Json -and $JsonFile) { throw '-Json and -JsonFile are the same body twice' }
if ($Attempts -lt 1) { throw '-Attempts is at least 1' }

# Split here, because a shell hands `-Select number,title` over as one string and PowerShell binds
# it whole; called from PowerShell the comma has already split it, and splitting again costs nothing.
$Select = @($Select | Where-Object { $_ } | ForEach-Object { $_.Split(',') } |
        ForEach-Object { $_.Trim() } | Where-Object { $_ })

$token = [Environment]::GetEnvironmentVariable('GH_TOKEN')
if (-not $token) { throw 'GH_TOKEN is not set in this environment' }

if ($JsonFile) { $Json = Get-Content -LiteralPath $JsonFile -Raw }
if ($MarkdownFile) {
    $properties = if ($Json) { $Json | ConvertFrom-Json -AsHashtable } else { @{} }
    if ($properties.ContainsKey('body')) { throw '-MarkdownFile is the body property the JSON already has' }
    $properties['body'] = Get-Content -LiteralPath $MarkdownFile -Raw
    $Json = $properties | ConvertTo-Json -Depth 100
}

if (-not $Method) { $Method = if ($Json) { 'POST' } else { 'GET' } }
$Method = $Method.ToUpperInvariant()

$uri = if ($Path -match '^https?://') { $Path }
elseif ($Path.StartsWith('/')) { "https://api.github.com$Path" }
else { "https://api.github.com/repos/$Repository/$Path" }

# Every call carries the token, so no other host is ever called: a full URL naming one would hand
# it over, plain http included.
$target = [uri] $uri
if ($target.Scheme -ne 'https' -or $target.Host -ne 'api.github.com') {
    throw "$uri is not https://api.github.com — the token goes to GitHub alone"
}

# 100 is the API's maximum: `issues?state=open` is one call, not a walk 30 at a time.
if ($Method -eq 'GET' -and $uri -notmatch '[?&]per_page=') {
    $uri += "$(if ($uri.Contains('?')) { '&' } else { '?' })per_page=100"
}

# HttpClient rather than Invoke-WebRequest: its timeout covers the body as well as the headers, so a
# server that answers one byte at a time ends the call instead of holding the round open.
$timeoutSeconds = 30
$client = [Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds($timeoutSeconds)

function Get-Header($Response, [string] $Name) {
    $values = $null
    if ($Response.Headers.TryGetValues($Name, [ref] $values)) { return @($values)[0] }
    return $null
}

function Invoke-Call([string] $CallUri) {
    for ($attempt = 1; ; $attempt++) {
        $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($Method), $CallUri)
        $request.Headers.Add('Authorization', "Bearer $token")
        $request.Headers.Add('Accept', 'application/vnd.github+json')
        $request.Headers.Add('X-GitHub-Api-Version', '2022-11-28')
        $request.Headers.Add('User-Agent', 'EvilCase-loop')
        if ($Json) {
            $request.Content = [Net.Http.ByteArrayContent]::new([Text.Encoding]::UTF8.GetBytes($Json))
            $request.Content.Headers.ContentType = [Net.Http.Headers.MediaTypeHeaderValue]::new('application/json')
        }

        try {
            # The status decides between a retry and an error, and HttpClient leaves it to us.
            $response = $client.Send($request)
        }
        catch {
            $failure = $_.Exception
            while ($failure -is [Management.Automation.MethodInvocationException]) { $failure = $failure.InnerException }
            if ($failure -is [OperationCanceledException]) {
                throw "$Method $CallUri gave up after $timeoutSeconds s, headers and body together"
            }
            throw "$Method ${CallUri}: $($failure.Message)"
        }

        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $status = [int] $response.StatusCode
        if ($status -ge 200 -and $status -lt 300) {
            return [pscustomobject] @{ Content = $content; Link = (Get-Header $response 'Link') }
        }

        $rejected = $status -eq 429 -or ($status -eq 403 -and $content -match 'rate limit')
        if (-not ($rejected -or ($status -ge 500 -and $Method -eq 'GET')) -or $attempt -ge $Attempts) {
            [Console]::Error.WriteLine("$Method $CallUri`n$content")
            throw "$Method $CallUri answered $status"
        }
        $after = Get-Header $response 'Retry-After'
        $wait = if ($after -as [int]) { [int] $after } else { [math]::Pow(2, $attempt) }
        # stderr: stdout carries the response.
        [Console]::Error.WriteLine("$status, attempt $attempt of $Attempts; retrying in $wait s")
        Start-Sleep -Seconds ([math]::Min(60, $wait))
    }
}

# Pages are followed on GET alone: no write endpoint paginates, and following one would repeat it.
# Only the `next` link's query is followed — its path names the repository by numeric id, which this
# environment's proxy refuses — so the path stays the one that answered the first page.
$maximumPages = 20
$pages = @(Invoke-Call $uri)
if ($Method -eq 'GET') {
    while ($true) {
        $link = $pages[-1].Link
        if (-not ($link -and $link -match '<([^>]+)>\s*;\s*rel="next"')) { break }
        # The server decides how long `next` goes on; the cap is what makes it stop.
        if ($pages.Count -ge $maximumPages) {
            throw "$uri still had a next page after $maximumPages; narrow it, or ask for a page at a time"
        }
        $pages += Invoke-Call ($uri.Split('?')[0] + ([uri] $Matches[1]).Query)
    }
}

if ($pages.Count -eq 1 -and -not $Select -and -not $Where) {
    if ($pages[0].Content) { Write-Output $pages[0].Content }
    return
}

# -DateKind String: `2026-08-05T13:35:34Z` is a [datetime] otherwise, and a column of it would be
# whatever the current culture makes of it, without the zone.
# The pipeline flattens each page's array into one, which is what a caller of a paginated GET asked
# for; a single object stays itself.
$data = @($pages | Where-Object { $_.Content } |
        ForEach-Object { $_.Content | ConvertFrom-Json -DateKind String })

# `labels.name='v1.0'.color` splits on the dots outside the quotes, so a value holding one is
# reachable; the quotes come off the value, never off a name.
function Split-SelectPath([string] $Name) {
    $segments = [Collections.Generic.List[string]]::new()
    $segment = ''
    $quoted = $false
    foreach ($character in $Name.ToCharArray()) {
        if ($character -eq "'") { $quoted = -not $quoted; $segment += $character; continue }
        if ($character -eq '.' -and -not $quoted) { $segments.Add($segment); $segment = ''; continue }
        $segment += $character
    }
    if ($quoted) { throw "$Name leaves a quote open" }
    $segments.Add($segment)

    $parts = [Collections.Generic.List[object]]::new()
    foreach ($text in $segments) {
        if (-not $text) { throw "$Name has an empty step" }
        $part = [pscustomobject] @{ Name = $text; Index = -1; Key = ''; Wanted = '' }
        if ($text -match '^\d+$') {
            $part.Index = [int] $text
        }
        elseif ($text -match '^([^=]+)=(.*)$') {
            $part.Key = $Matches[1]
            $wanted = $Matches[2]
            if ($wanted.Length -ge 2 -and $wanted.StartsWith("'") -and $wanted.EndsWith("'")) {
                $wanted = $wanted.Substring(1, $wanted.Length - 2)
            }
            $part.Wanted = $wanted
        }
        $parts.Add($part)
    }
    return $parts.ToArray()
}

$notes = [Collections.Generic.HashSet[string]]::new()

function New-Resolved($Value) { [pscustomobject] @{ Found = $true; Value = $Value; Missing = '' } }
function New-Unresolved([string] $Reason) { [pscustomobject] @{ Found = $false; Value = $null; Missing = $Reason } }

# What a path finds, or what it could not: -Select fails on the second, -Where answers with it.
function Resolve-Field($Item, [object[]] $Parts, [int] $Index) {
    if ($Index -ge $Parts.Count -or $null -eq $Item) { return New-Resolved $Item }
    $part = $Parts[$Index]

    if ($Item -is [Collections.IList]) {
        if ($part.Index -ge 0) {
            if ($part.Index -ge $Item.Count) { return New-Unresolved "no element $($part.Index) among $($Item.Count)" }
            return Resolve-Field $Item[$part.Index] $Parts ($Index + 1)
        }
        if ($part.Key) {
            $carrying = @($Item | Where-Object { $_.PSObject.Properties[$part.Key] })
            $matched = @($carrying | Where-Object { [string] $_.($part.Key) -eq $part.Wanted })
            if ($matched.Count) { return Resolve-Field $matched[0] $Parts ($Index + 1) }
            if ($Item.Count -and -not $carrying.Count) {
                return New-Unresolved "no element carries $($part.Key); one has: $(($Item[0].PSObject.Properties.Name | Sort-Object) -join ', ')"
            }
            if ($carrying.Count) {
                $values = ($carrying | ForEach-Object { [string] $_.($part.Key) } | Sort-Object -Unique) -join ', '
                [void] $script:notes.Add("$($part.Name) matched nothing; the $($part.Key) values are: $values")
            }
            return New-Resolved $null
        }
        # Any other name maps over the elements, so `labels.name` costs a column, not the array.
        $mapped = [Collections.Generic.List[object]]::new()
        foreach ($element in $Item) {
            $result = Resolve-Field $element $Parts $Index
            if (-not $result.Found) { return $result }
            $mapped.Add($result.Value)
        }
        return New-Resolved $mapped.ToArray()
    }

    if ($part.Index -ge 0 -or $part.Key) { return New-Unresolved "$($part.Name) wants an array" }
    $property = $Item.PSObject.Properties[$part.Name]
    if (-not $property) {
        return New-Unresolved "no $($part.Name); it has: $(($Item.PSObject.Properties.Name | Sort-Object) -join ', ')"
    }
    return Resolve-Field $property.Value $Parts ($Index + 1)
}

function Format-Column($Value) {
    if ($null -eq $Value) { return '' }
    if ($Value -is [bool]) { return $Value.ToString().ToLowerInvariant() }
    # A collection goes through -InputObject: piped, an empty one prints nothing and a single
    # element loses its brackets, and the column would change shape with the data.
    if (-not ($Value -is [string] -or $Value -is [ValueType])) {
        return (ConvertTo-Json -InputObject $Value -Depth 100 -Compress)
    }
    $text = if ($Value -is [string]) { $Value } else { [Convert]::ToString($Value, [Globalization.CultureInfo]::InvariantCulture) }
    return $text.Replace('\', '\\').Replace("`r", '\r').Replace("`n", '\n').Replace("`t", '\t')
}

if ($Where) {
    $negated = $Where.StartsWith('!')
    $test = Split-SelectPath $Where.TrimStart('!').Trim()
    $data = @($data | Where-Object {
            $result = Resolve-Field $_ $test 0
            $present = $result.Found -and $null -ne $result.Value
            if ($negated) { -not $present } else { $present }
        })
    # -Where is asking; what it did not find is its answer, not something to report.
    $notes.Clear()
}

if (-not $Select) {
    # -InputObject, so pages holding one element between them still print as an array.
    Write-Output (ConvertTo-Json -InputObject $data -Depth 100)
    return
}

$paths = [Collections.Generic.List[object[]]]::new()
# @(), because a one-step path comes back as the step itself and the list takes arrays.
foreach ($name in $Select) { $paths.Add(@(Split-SelectPath $name)) }

# Collected, then written: a name the last row cannot answer must not leave the rows before it on
# stdout, where they read as the whole result.
$rows = [Collections.Generic.List[string]]::new()
foreach ($item in $data) {
    $columns = [string[]]::new($Select.Count)
    for ($index = 0; $index -lt $Select.Count; $index++) {
        $result = Resolve-Field $item $paths[$index] 0
        if (-not $result.Found) { throw "$($Select[$index]): $($result.Missing)" }
        $columns[$index] = Format-Column $result.Value
    }
    $rows.Add($columns -join "`t")
}
# stderr, once each: the column is empty either way, and only the note says which emptiness it is.
foreach ($note in $notes) { [Console]::Error.WriteLine($note) }
Write-Output $rows.ToArray()
