#!/usr/bin/env pwsh
<#
    SessionStart hook: brings up the throwaway PostgreSQL of deploy/docker-compose.dev.yml when
    nothing answers on 127.0.0.1:5432. Takes no parameters. Skips the remote container, which
    starts its own server from session-start.sh. Never fails the session: a missing Docker or a
    compose that does not come up is a line on stdout and exit 0.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-PostgresPort {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        # Three seconds: the first socket call of a cold pwsh took longer than one.
        return $client.ConnectAsync('127.0.0.1', 5432).Wait(3000)
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

try {
    if ($env:CLAUDE_CODE_REMOTE -eq 'true') { exit 0 }
    if (Test-PostgresPort) { exit 0 }

    $repo = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { Join-Path $PSScriptRoot '..' '..' }
    $compose = Join-Path $repo 'deploy' 'docker-compose.dev.yml'

    docker compose -f $compose up -d --wait --wait-timeout 120 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "docker compose exited $LASTEXITCODE" }

    Write-Output '[session-start] PostgreSQL of deploy/docker-compose.dev.yml is up'
}
catch {
    Write-Output "[session-start] nothing answers on 127.0.0.1:5432 and Docker did not start one, so the tests and the host have no database: $_"
}

exit 0
