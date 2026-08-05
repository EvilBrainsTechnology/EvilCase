#!/usr/bin/env pwsh
<#
    Runs the host on a port and a database of its own, so parallel agents cannot take each
    other's; any number of runs share one checkout. Prints the URL, and nothing else, on stdout.

        ./.claude/skills/run-app/Start-EvilCase.ps1                    # prints https://localhost:<port>
        ./.claude/skills/run-app/Start-EvilCase.ps1 -Stop -Port 41449
        ./.claude/skills/run-app/Start-EvilCase.ps1 -Stop -All         # every run of this checkout

    -Stop kills the host, drops the database and removes the run's files. It takes -Port, the port
    the start printed — the runs beside it belong to other agents. -All is for the checkout, not
    for one's own run. A start that failed needs -Stop too: whatever it got as far as creating is
    still there, and it says so.

    -PostgresHost, -PostgresPort, -PostgresUser and -PostgresPassword reach a server other than
    localhost:5432 as postgres/postgres. -ReadyTimeoutSeconds bounds the wait for /health/ready.

    A run's state and the host's output live in ~/.evilcase-runs/<checkout>/<port>.{json,log,err},
    with the build's in build.log beside them — outside the checkout, which may be a worktree
    that is removed while the host it started is still running.
#>
param(
    [switch] $Stop,
    [switch] $All,
    [int] $Port,
    [string] $PostgresHost = 'localhost',
    [int] $PostgresPort = 5432,
    [string] $PostgresUser = 'postgres',
    [string] $PostgresPassword = 'postgres',
    [int] $ReadyTimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Refused, never prompted for: a missing mandatory parameter reads stdin, and a tool call with no
# one at the keyboard hangs on it.
if ($Stop -and -not ([bool] $Port -xor [bool] $All)) { throw '-Stop takes either -Port <port> or -All' }
if (-not $Stop -and ($Port -or $All)) { throw '-Port and -All belong to -Stop' }

if (Test-Path 'variable:PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

# From the script's own location, never the caller's directory: run from another repository, the
# git root is that one, and a start gets as far as creating a database before missing src/.
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..' '..' '..')).Path
if (-not (Test-Path -LiteralPath (Join-Path $root 'src'))) { throw "$root is not an EvilCase checkout" }

$checkout = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($root))).Substring(0, 8).ToLowerInvariant()
$stateDirectory = Join-Path $HOME '.evilcase-runs' "$(Split-Path -Path $root -Leaf)-$checkout"
$env:PGPASSWORD = $PostgresPassword

function Invoke-Postgres([string] $Tool, [string[]] $ToolArguments) {
    $output = & $Tool -h $PostgresHost -p $PostgresPort -U $PostgresUser @ToolArguments
    if ($LASTEXITCODE -ne 0) { throw "$Tool failed with exit code $LASTEXITCODE" }
    return $output
}

function Test-Database([string] $Name) {
    return @(Invoke-Postgres 'psql' @('-d', 'postgres', '-Atc', "select 1 from pg_database where datname = '$Name'")) -contains '1'
}

# A refused connection is the only proof the host let go of the port: one that hangs, on a backlog
# nobody is accepting from, is a port still held. WaitAny, because Wait rethrows the refusal.
function Test-Port([int] $Number) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connect = $client.ConnectAsync([System.Net.IPAddress]::Loopback, $Number)
        if ([System.Threading.Tasks.Task]::WaitAny(@($connect), 1000) -lt 0) { return $true }
        return -not ($connect.Exception -and ($connect.Exception.InnerExceptions | Where-Object {
                    $_ -is [System.Net.Sockets.SocketException] -and $_.SocketErrorCode -eq 'ConnectionRefused' }))
    }
    finally { $client.Dispose() }
}

# stderr: stdout carries the URL.
function Write-Diagnostics([string[]] $Paths) {
    foreach ($path in $Paths | Where-Object { Test-Path -LiteralPath $_ }) {
        $tail = @(Get-Content -LiteralPath $path -Tail 20)
        if ($tail) { [Console]::Error.WriteLine("$path`n$($tail -join [Environment]::NewLine)") }
    }
}

if ($Stop) {
    $states = @()
    if (Test-Path -LiteralPath $stateDirectory) {
        $states = @(Get-ChildItem -LiteralPath $stateDirectory -Filter '*.json' |
                Where-Object { $All -or $_.BaseName -eq [string] $Port })
    }
    if (-not $states) { Write-Warning "no run$(if ($Port) { " on port $Port" }) recorded in $stateDirectory" }

    # One run per iteration, each inside its own try: a state file a SIGKILL truncated mid-write, or
    # a run that will not go down, costs only itself — the rest of the checkout still stops.
    $kept = @()
    foreach ($stateFile in $states) {
        try {
            $state = Get-Content -LiteralPath $stateFile.FullName -Raw | ConvertFrom-Json
            $done = @()

            # By pid, and only while it is still the command line this run started: a pid is recycled,
            # and a state file left behind by a crash has had an unrelated process killed by now.
            $process = Get-Process -Id $state.Pid -ErrorAction SilentlyContinue
            if ($process -and $process.CommandLine -eq $state.CommandLine) {
                Stop-Process -InputObject $process -Force
                $done += "killed host $($state.Pid)"
            }
            elseif ($process) { $done += "left pid $($state.Pid) alone, it is not this run's host" }
            else { $done += "host $($state.Pid) was gone" }

            # Only then the database: dropping it under a host still serving takes the schema away
            # from it, and removing the state file leaves nothing naming either.
            $deadline = [datetime]::UtcNow.AddSeconds(30)
            while ((Test-Port $state.Port) -and [datetime]::UtcNow -lt $deadline) { Start-Sleep -Seconds 1 }
            if (Test-Port $state.Port) { throw "$($state.Url) still answers, so $($state.Database) stays" }

            if (Test-Database $state.Database) {
                # --force: the connection outlives the process by a moment and a plain dropdb fails on it.
                Invoke-Postgres 'dropdb' @('--force', $state.Database) | Out-Null
                $done += "dropped $($state.Database)"
            }
            else { $done += "$($state.Database) was gone" }

            Remove-Item -LiteralPath $stateFile.FullName, $state.Log, $state.ErrorLog -Force -ErrorAction SilentlyContinue
            Write-Output "$($state.Url): $($done -join ', ')"
        }
        catch { $kept += "$($stateFile.FullName) kept: $($_.Exception.Message)" }
    }

    # With the last run goes what is left of the checkout's directory, the build log included.
    if ((Test-Path -LiteralPath $stateDirectory) -and
        -not (Get-ChildItem -LiteralPath $stateDirectory -Filter '*.json')) {
        Remove-Item -LiteralPath $stateDirectory -Recurse -Force
    }
    if ($kept) { throw ($kept -join [Environment]::NewLine) }
    return
}

New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null

# Built once and started from the assembly: `dotnet r run` puts five launchers between the caller
# and Kestrel, so the pid to record is not the one holding the port. Parallel starts share the
# checkout's obj/ and bin/, so they take turns.
$buildLog = Join-Path $stateDirectory 'build.log'
$buildLock = [System.Threading.Mutex]::new($false, "evilcase-build-$checkout")
try { $buildLock.WaitOne() | Out-Null } catch [System.Threading.AbandonedMutexException] { }
try {
    $build = Start-Process -FilePath 'dotnet' -ArgumentList @('r', 'build') -WorkingDirectory (Join-Path $root 'src') `
        -RedirectStandardOutput $buildLog -RedirectStandardError "$buildLog.err" -PassThru -Wait
}
finally {
    $buildLock.ReleaseMutex()
    $buildLock.Dispose()
}
if ($build.ExitCode -ne 0) {
    Write-Diagnostics @($buildLog, "$buildLog.err")
    throw "dotnet r build failed with exit code $($build.ExitCode) — see $buildLog"
}

$assembly = Join-Path $root 'src/EvilCase.Host/bin/Release/net10.0/EvilBrains.EvilCase.Host.dll'
if (-not (Test-Path -LiteralPath $assembly)) { throw "the build produced no $assembly" }

# Port 0 binds whatever is free; the window between closing it and Kestrel binding is the price of
# asking the OS. The unsafe ports are the ones a browser refuses to open, ephemeral or not.
$unsafePorts = @(1719, 1720, 1723, 2049, 3659, 4045, 5060, 5061, 6000, 6566, 6665, 6666, 6667, 6668, 6669, 6697, 10080)
do {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $stateFile = Join-Path $stateDirectory "$port.json"
} while ($unsafePorts -contains $port -or (Test-Path -LiteralPath $stateFile))

$database = "evilcase_$port"
$url = "https://localhost:$port"
$log = Join-Path $stateDirectory "$port.log"
$errorLog = Join-Path $stateDirectory "$port.err"

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:EvilBrains__EvilCase__ConnectionString =
"Host=$PostgresHost;Port=$PostgresPort;Database=$database;Username=$PostgresUser;Password=$PostgresPassword"

Invoke-Postgres 'createdb' @($database) | Out-Null
$hostProcess = $null
try {
    $hostProcess = Start-Process -FilePath 'dotnet' -ArgumentList @($assembly, '--urls', $url) `
        -WorkingDirectory (Split-Path -Path $assembly -Parent) `
        -RedirectStandardOutput $log -RedirectStandardError $errorLog -PassThru

    [ordered] @{
        Pid = $hostProcess.Id
        CommandLine = $hostProcess.CommandLine
        Port = $port
        Database = $database
        Url = $url
        Log = $log
        ErrorLog = $errorLog
    } | ConvertTo-Json | Set-Content -LiteralPath $stateFile
}
catch {
    # Until the state file is written nothing names the database, and -Stop would never find it.
    if ($hostProcess) { Stop-Process -InputObject $hostProcess -Force -ErrorAction SilentlyContinue }
    Invoke-Postgres 'dropdb' @('--force', '--if-exists', $database) | Out-Null
    throw
}

$stopCommand = "$PSCommandPath -Stop -Port $port"
$deadline = [datetime]::UtcNow.AddSeconds($ReadyTimeoutSeconds)
while ([datetime]::UtcNow -lt $deadline) {
    if ($hostProcess.HasExited) {
        Write-Diagnostics @($log, $errorLog)
        throw "the host exited with code $($hostProcess.ExitCode) — $database is still there: $stopCommand"
    }
    try {
        if ((Invoke-RestMethod -Uri "$url/health/ready" -SkipCertificateCheck -TimeoutSec 5).status -eq 'Healthy') {
            Write-Output $url
            return
        }
    }
    catch { }
    Start-Sleep -Seconds 2
}

Write-Diagnostics @($log, $errorLog)
throw "the host did not answer $url/health/ready within $ReadyTimeoutSeconds s — it and $database are still there: $stopCommand"
