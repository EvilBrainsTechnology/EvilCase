#!/usr/bin/env pwsh
<#
    Starts docker-compose.local.yml for this checkout and prints the address it ended up on.
    The compose project and the host port are derived from the checkout's path, so worktrees run
    side by side without sharing containers, an image or an address. -Stop removes the stack.
#>
param([switch] $Stop)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Test-Path 'variable:PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) {
    Write-Output 'not inside a git repository'
    exit 1
}

$digest = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($root.ToLowerInvariant()))
$project = 'evilcase-local-' + [System.Convert]::ToHexString($digest).Substring(0, 8).ToLowerInvariant()
$compose = @('compose', '--project-name', $project, '--file', (Join-Path $root 'deploy/docker-compose.local.yml'))

if ($Stop) {
    docker @compose down
    exit $LASTEXITCODE
}

# --wait returns once the image's health check passes, so nothing is verified against a starting
# application; --build makes every start serve the working tree rather than the last image.
docker @compose up --build --detach --wait
if ($LASTEXITCODE -ne 0) {
    docker @compose logs --tail 40 app
    exit $LASTEXITCODE
}

$address = (docker @compose port app 8080) -replace '^0\.0\.0\.0|^127\.0\.0\.1', 'localhost'
Write-Output "EvilCase on http://$address, compose project $project"
