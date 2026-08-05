#!/usr/bin/env pwsh
<#
    One GitHub REST call as `claude[bot]`, and the response on stdout. It exists because a
    worktree-isolated agent's shell refuses a command that expands $GH_TOKEN: the expansion lives
    here, and every call goes through it. The endpoints are in github-api.md beside it.

        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 pulls/12 -Select number,mergeable_state
        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 issues/12/comments -MarkdownFile /tmp/x/reply.md
        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 issues/12/labels -Json '{"labels":["blocked"]}'
        ./.claude/skills/product-loop/Invoke-EvilCaseGitHub.ps1 issues/12 -Method PATCH -Json '{"state":"closed"}'

    -Path is relative to the repository (`pulls/12`); one starting with / is relative to
    api.github.com and a full URL stands as it is. -Method defaults to GET, or to POST when a body
    is given.

    The body is JSON, from -Json on the command line or -JsonFile for what does not fit on one.
    -MarkdownFile fills the `body` property from a file of plain markdown — a comment, a reply or a
    pull request description is written as markdown and never escaped by hand, backticks, $, quotes
    and newlines included. It combines with -Json or -JsonFile carrying the other properties, and
    is refused when those already name `body`.

    stdout is the response, verbatim. -Select prints named properties instead — one tab-separated
    column per name, one line per element of an array — so `-Select number` after opening a pull
    request is its number and nothing else, with no second call. A dotted path walks into nested
    objects; a number in it takes an array's element and `name=value` the element carrying it:
    `issue_field_values.issue_field_name=Priority.single_select_option.name` is that field's value.
    A column is one line whatever the data holds: a missing value is empty, an object or an array
    is compact JSON — `[]` and `[{…}]` included — and a string's backslashes, newlines and tabs
    come out as \\, \n and \t. A GET is followed through its `Link: rel="next"` pages; only then is
    the response reformatted, as the joined array.

    A non-2xx puts the status and the response on stderr and fails. Two of them are retried instead,
    up to -Attempts times, waiting what `Retry-After` says or 2, 4, 8 … seconds: a 429 or a secondary
    rate limit whatever the method, both of which mean nothing was written, and a 5xx on GET alone,
    where a repeat cannot write twice. A missing $GH_TOKEN, and -Json together with -JsonFile, fail
    before the call; an unknown -Select property fails after it, naming what the response does have.
#>
param(
    [Parameter(Position = 0)] [string] $Path,
    [string] $Method,
    [string] $Json,
    [string] $JsonFile,
    [string] $MarkdownFile,
    [string[]] $Select,
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

$headers = @{
    Authorization = "Bearer $token"
    Accept = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
}

function Get-Header($Response, [string] $Name) {
    $key = @($Response.Headers.Keys | Where-Object { $_ -ieq $Name })
    if (-not $key) { return $null }
    return @($Response.Headers[$key[0]])[0]
}

# -SkipHttpErrorCheck: the status decides between a retry and an error, and the thrown exception
# carries the response only as text.
function Invoke-Call([string] $CallUri) {
    for ($attempt = 1; ; $attempt++) {
        $arguments = @{
            Uri = $CallUri
            Method = $Method
            Headers = $headers
            SkipHttpErrorCheck = $true
        }
        if ($Json) {
            $arguments.Body = [Text.Encoding]::UTF8.GetBytes($Json)
            $arguments.ContentType = 'application/json'
        }
        $response = Invoke-WebRequest @arguments
        $status = [int] $response.StatusCode
        if ($status -ge 200 -and $status -lt 300) { return $response }

        $rejected = $status -eq 429 -or ($status -eq 403 -and $response.Content -match 'rate limit')
        if (-not ($rejected -or ($status -ge 500 -and $Method -eq 'GET')) -or $attempt -ge $Attempts) {
            [Console]::Error.WriteLine("$Method $CallUri`n$($response.Content)")
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
$pages = @(Invoke-Call $uri)
if ($Method -eq 'GET') {
    while ($true) {
        $link = Get-Header $pages[-1] 'Link'
        if (-not ($link -and $link -match '<([^>]+)>\s*;\s*rel="next"')) { break }
        $pages += Invoke-Call ($uri.Split('?')[0] + ([uri] $Matches[1]).Query)
    }
}

if ($pages.Count -eq 1 -and -not $Select) {
    if ($pages[0].Content) { Write-Output $pages[0].Content }
    return
}

# The pipeline flattens each page's array into one, which is what a caller of a paginated GET asked
# for; a single object stays itself.
$data = @($pages | Where-Object { $_.Content } | ForEach-Object { $_.Content | ConvertFrom-Json })
if (-not $Select) {
    Write-Output ($data | ConvertTo-Json -Depth 100)
    return
}

function Get-Field($Item, [string] $Name) {
    $value = $Item
    foreach ($part in $Name.Split('.')) {
        if ($null -eq $value) { return '' }
        if ($value -is [Collections.IList]) {
            if ($part -match '^\d+$') {
                $index = [int] $part
                $value = if ($index -lt $value.Count) { $value[$index] } else { $null }
                continue
            }
            if ($part -match '^([^=]+)=(.*)$') {
                $key = $Matches[1]
                $wanted = $Matches[2]
                $matched = @($value | Where-Object { $_.PSObject.Properties[$key] -and [string] $_.$key -eq $wanted })
                $value = if ($matched.Count) { $matched[0] } else { $null }
                continue
            }
        }
        $property = $value.PSObject.Properties[$part]
        if (-not $property) {
            throw "the response has no $part; it has: $(($value.PSObject.Properties.Name | Sort-Object) -join ', ')"
        }
        $value = $property.Value
    }
    if ($null -eq $value) { return '' }
    if ($value -is [bool]) { return $value.ToString().ToLowerInvariant() }
    # A collection goes through -InputObject: piped, an empty one prints nothing and a single
    # element loses its brackets, and the column would change shape with the data.
    if (-not ($value -is [string] -or $value -is [ValueType])) {
        return (ConvertTo-Json -InputObject $value -Depth 100 -Compress)
    }
    return ([string] $value).Replace('\', '\\').Replace("`r", '\r').Replace("`n", '\n').Replace("`t", '\t')
}

# Indexed rather than piped: a name whose value yields nothing must still print its column.
foreach ($item in $data) {
    $columns = [string[]]::new($Select.Count)
    for ($index = 0; $index -lt $Select.Count; $index++) {
        $columns[$index] = Get-Field $item $Select[$index]
    }
    Write-Output ($columns -join "`t")
}
