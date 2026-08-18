<#
.SYNOPSIS
  Run Cursor Usage Progress for local development.

.PARAMETER Background
  Launch in tray-only mode (--background).

.PARAMETER Configuration
  Build configuration. Debug (default) or Release.

.PARAMETER Test
  Run unit tests instead of launching the app.
#>
[CmdletBinding()]
param(
    [switch]$Background,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [switch]$Test
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

if ($Test) {
    Write-Host "Running tests ($Configuration)..."
    dotnet test .\Tests\CursorUsageProgress.Tests.csproj -c $Configuration
    exit $LASTEXITCODE
}

Write-Host "Starting Cursor Usage Progress ($Configuration)..."
$runArgs = @(
    'run',
    '--project', '.\CursorUsageProgress.csproj',
    '-c', $Configuration
)
if ($Background) {
    $runArgs += '--', '--background'
}

dotnet @runArgs
exit $LASTEXITCODE
