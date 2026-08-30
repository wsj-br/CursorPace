<#
.SYNOPSIS
  Remove generated build, test, and leftover WinUI artifacts from the repository.

.DESCRIPTION
  Runs dotnet clean, then removes generated directories and files that can survive
  a normal clean. Source files and project data are not touched.

.PARAMETER Configuration
  Build configuration passed to dotnet clean.

.PARAMETER PurgeNuGetCache
  Clear local NuGet caches by default. Use -PurgeNuGetCache:$false to skip. Packages must be downloaded again on the next restore.

.PARAMETER PurgeUserTemp
  Remove CursorPace-related files from the current user's TEMP folder by default. Use -PurgeUserTemp:$false to skip.

.EXAMPLE
  .\scripts\clean.ps1

.EXAMPLE
  .\scripts\clean.ps1 -PurgeNuGetCache

.EXAMPLE
  .\scripts\clean.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [bool]$PurgeNuGetCache = $true,

    [bool]$PurgeUserTemp = $true,

    [switch]$DryRun
)

$ErrorActionPreference = 'Continue'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $RepoRoot

function Remove-GeneratedPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    if (-not $DryRun) {
        Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Continue
    }
}

function Remove-GeneratedFile {
    param(
        [Parameter(Mandatory = $true)][System.IO.FileInfo]$File,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if (-not $DryRun) {
        Remove-Item -LiteralPath $File.FullName -Force -ErrorAction Continue
    }
}

Write-Host "Cleaning Cursor Pace ($Configuration)..."
if (-not $DryRun) {
    & dotnet clean .\CursorPace.csproj -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "dotnet clean exited with code $LASTEXITCODE. Continuing with forced artifact cleanup."
    }
}

$generatedDirectories = @(
    'bin',
    'obj',
    'Tests\bin',
    'Tests\obj',
    '.vs',
    'TestResults',
    'artifacts',
    '_build-check'
)

foreach ($relativePath in $generatedDirectories) {
    Remove-GeneratedPath `
        -Path (Join-Path $RepoRoot $relativePath) `
        -Description 'generated directory'
}

$excludedDirectories = @(
    '.git',
    'bin',
    'obj',
    '.vs',
    'TestResults',
    'artifacts',
    '_build-check',
    'installer'
)

# .xbf / .pri are leftover WinUI artifacts that can linger beside custom -o folders.
$generatedExtensions = @(
    '.xbf',
    '.pri',
    '.tmp',
    '.tlog',
    '.trx',
    '.coverage',
    '.coveragexml',
    '.pdb'
)

$files = Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Force -ErrorAction SilentlyContinue |
    Where-Object {
        $relativePath = $_.FullName.Substring($RepoRoot.Length).TrimStart('\', '/')
        $pathParts = $relativePath -split '[\\/]'
        ($pathParts | Where-Object { $excludedDirectories -contains $_ }).Count -eq 0 -and
        $generatedExtensions -contains $_.Extension.ToLowerInvariant()
    }

foreach ($file in $files) {
    Remove-GeneratedFile -File $file -Description 'generated file'
}

if ($PurgeNuGetCache) {
    Write-Host 'Clearing local NuGet caches...'
    if (-not $DryRun) {
        & dotnet nuget locals all --clear
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "dotnet nuget locals exited with code $LASTEXITCODE."
        }
    }
}

if ($PurgeUserTemp) {
    $tempRoot = [System.IO.Path]::GetTempPath()
    Get-ChildItem -LiteralPath $tempRoot -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like '*CursorPace*' -or $_.Name -like '*XamlCompiler*' } | # *XamlCompiler* is a leftover WinUI temp-name pattern
        ForEach-Object {
            Remove-GeneratedPath -Path $_.FullName -Description 'project temporary data'
        }
}

Write-Host 'Workspace cleanup complete.'






