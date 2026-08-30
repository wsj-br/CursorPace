<#
.SYNOPSIS
  Run Cursor Pace for local development.

.PARAMETER Background
  Launch in tray-only mode (--background).

.PARAMETER Show
  Force the main window open (--show). Wins over -Background.

.PARAMETER Configuration
  Build configuration. Debug (default) or Release.

.PARAMETER Test
  Run unit tests instead of launching the app.
#>
[CmdletBinding()]
param(
    [switch]$Background,

    [switch]$Show,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [switch]$Test
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

if ($Test) {
    Write-Host "Running tests ($Configuration)..."
    dotnet test .\Tests\CursorPace.Tests.csproj -c $Configuration
    exit $LASTEXITCODE
}

Write-Host "Starting Cursor Pace ($Configuration)..."
$runArgs = @(
    'run',
    '--project', '.\CursorPace.csproj',
    '-c', $Configuration
)
if ($Show) {
    $runArgs += '--', '--show'
}
elseif ($Background) {
    $runArgs += '--', '--background'
}

dotnet @runArgs
exit $LASTEXITCODE
