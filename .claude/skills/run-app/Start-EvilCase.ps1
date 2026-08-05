#!/usr/bin/env pwsh
<#
    Runs the host on a port and a database of its own, so parallel agents cannot take each
    other's. Prints the URL; -Stop kills the host and drops the database again.

        ./.claude/skills/run-app/Start-EvilCase.ps1            # prints https://localhost:<port>
        ./.claude/skills/run-app/Start-EvilCase.ps1 -Stop

    The state lives in .evilcase-run.json at the repository root, so -Stop needs no arguments.
#>
[CmdletBinding()]
param(
    [switch] $Stop,
    [string] $PostgresHost = 'localhost',
    [int] $PostgresPort = 5432,
    [string] $PostgresUser = 'postgres',
    [string] $PostgresPassword = 'postgres',
    [int] $ReadyTimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Test-Path 'variable:PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) { throw 'not inside a git repository' }
Set-Location -LiteralPath $root
$stateFile = Join-Path $root '.evilcase-run.json'
$env:PGPASSWORD = $PostgresPassword

function Invoke-Postgres([string] $Tool, [string[]] $ToolArguments) {
    & $Tool -h $PostgresHost -p $PostgresPort -U $PostgresUser @ToolArguments
    if ($LASTEXITCODE -ne 0) { throw "$Tool failed with exit code $LASTEXITCODE" }
}

if ($Stop) {
    if (-not (Test-Path -LiteralPath $stateFile)) { throw "nothing to stop: $stateFile is not there" }
    $state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json

    # By pid: `pkill -f EvilCase.Host` also matches the shell that started it and kills the session.
    Stop-Process -Id $state.Pid -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    # --force: the connection outlives the process by a moment and a plain dropdb fails on it.
    Invoke-Postgres 'dropdb' @('--force', '--if-exists', $state.Database)
    # -Force: a leading dot makes it hidden to PowerShell, which then refuses to remove it.
    Remove-Item -LiteralPath $stateFile -Force
    Write-Output "stopped $($state.Url), dropped $($state.Database)"
    return
}

if (Test-Path -LiteralPath $stateFile) {
    throw "a run is already recorded in $stateFile — stop it first"
}

# Port 0 binds whatever is free; an ephemeral one is above the browsers' unsafe-port list by
# construction. The window between closing it and Kestrel binding is the price of asking the OS.
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()

$database = "evilcase_$port"
$url = "https://localhost:$port"
Invoke-Postgres 'createdb' @($database)

$env:EvilBrains__EvilCase__ConnectionString =
"Host=$PostgresHost;Port=$PostgresPort;Database=$database;Username=$PostgresUser;Password=$PostgresPassword"

# --urls, not ASPNETCORE_URLS: applicationUrl in launchSettings.json wins over the variable and
# the host binds 5000 anyway.
$log = Join-Path $root '.evilcase-run.log'
$host_ = Start-Process -FilePath 'dotnet' `
    -ArgumentList @('r', 'run', '--', '--urls', $url) `
    -WorkingDirectory (Join-Path $root 'src') `
    -RedirectStandardOutput $log -RedirectStandardError "$log.err" -PassThru

@{ Pid = $host_.Id; Port = $port; Database = $database; Url = $url; Log = $log } |
    ConvertTo-Json | Set-Content -LiteralPath $stateFile

$deadline = [datetime]::UtcNow.AddSeconds($ReadyTimeoutSeconds)
while ([datetime]::UtcNow -lt $deadline) {
    if ($host_.HasExited) {
        Get-Content -LiteralPath $log -Tail 20 | Write-Output
        throw "the host exited with code $($host_.ExitCode) — see $log"
    }
    try {
        $ready = Invoke-RestMethod -Uri "$url/health/ready" -SkipCertificateCheck -TimeoutSec 5
        if ($ready.status -eq 'Healthy') {
            Write-Output $url
            return
        }
    }
    catch { Start-Sleep -Seconds 2 }
}

throw "the host did not answer $url/health/ready within $ReadyTimeoutSeconds s — see $log"
