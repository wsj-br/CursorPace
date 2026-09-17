<#
.SYNOPSIS
  Update NuGet PackageReference versions and refresh lock files.

.DESCRIPTION
  With no package list, updates every direct PackageReference in the app
  and test projects to the highest version on the configured sources.
  Named packages are updated only in the project that references them.
  Restores the solution with --force-evaluate so both lock files stay in sync.

.PARAMETER List
  List outdated packages and exit (no writes). With -Vulnerable, list
  vulnerable packages instead.

.PARAMETER Vulnerable
  Lift only packages NuGet Audit reports, to the lowest safe version.

.PARAMETER Test
  Run unit tests after the restore.

.PARAMETER Packages
  Package ids, optionally with @version (for example Avalonia@12.1.2).

.EXAMPLE
  .\scripts\update-packages.ps1

.EXAMPLE
  .\scripts\update-packages.ps1 -List

.EXAMPLE
  .\scripts\update-packages.ps1 xunit

.EXAMPLE
  .\scripts\update-packages.ps1 Avalonia@12.1.2
#>
[CmdletBinding()]
param(
    [Alias('l')]
    [switch]$List,

    [switch]$Vulnerable,

    [Alias('t')]
    [switch]$Test,

    [switch]$Help,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Packages
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$SolutionPath = Join-Path $RepoRoot 'CursorPace.slnx'
$AppProject = Join-Path $RepoRoot 'CursorPace.csproj'
$TestProject = Join-Path $RepoRoot 'Tests\CursorPace.Tests.csproj'
$AvaloniaAligned = @(
    'Avalonia',
    'Avalonia.Desktop',
    'Avalonia.Themes.Fluent',
    'Avalonia.Fonts.Inter'
)

function Fail {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "Error: $Message" -ForegroundColor Red
    exit 1
}

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$DotNetArgs)
    & dotnet @DotNetArgs
    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet $($DotNetArgs -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-PackageId {
    param([Parameter(Mandatory = $true)][string]$Spec)
    $at = $Spec.IndexOf('@')
    if ($at -lt 0) {
        return $Spec
    }
    return $Spec.Substring(0, $at)
}

function Test-ProjectHasPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Project,
        [Parameter(Mandatory = $true)][string]$Id
    )
    $raw = [System.IO.File]::ReadAllText($Project)
    return $raw -match ('<PackageReference\s+Include="' + [regex]::Escape($Id) + '"')
}

function Get-PackageVersion {
    param(
        [Parameter(Mandatory = $true)][string]$Project,
        [Parameter(Mandatory = $true)][string]$Id
    )
    $raw = [System.IO.File]::ReadAllText($Project)
    $match = [regex]::Match($raw, '<PackageReference Include="' + [regex]::Escape($Id) + '" Version="([^"]+)"')
    if (-not $match.Success) {
        return $null
    }
    return $match.Groups[1].Value
}

function Get-MatchingSpecs {
    param([Parameter(Mandatory = $true)][string]$Project)
    $matching = @()
    foreach ($spec in $Packages) {
        $id = Get-PackageId -Spec $spec
        if (Test-ProjectHasPackage -Project $Project -Id $id) {
            $matching += $spec
        }
    }
    return $matching
}

function Update-ProjectPackages {
    param(
        [Parameter(Mandatory = $true)][string]$Project,
        [Parameter(Mandatory = $true)][string]$Relative
    )

    $dotnetArgs = @('package', 'update', '--project', $Project)
    if ($Vulnerable) {
        $dotnetArgs += '--vulnerable'
    }

    if ($Packages -and $Packages.Count -gt 0) {
        $matching = @(Get-MatchingSpecs -Project $Project)
        if ($matching.Count -eq 0) {
            Write-Host "Skipping $Relative (no matching PackageReference)."
            return $false
        }
        $dotnetArgs += $matching
    }

    Write-Host "Updating $Relative..."
    Invoke-DotNet -DotNetArgs $dotnetArgs
    return $true
}

function Test-AvaloniaAlignment {
    Write-Host 'Checking Avalonia package alignment...'
    $first = $null
    $mismatched = $false
    foreach ($id in $AvaloniaAligned) {
        $version = Get-PackageVersion -Project $AppProject -Id $id
        if ([string]::IsNullOrWhiteSpace($version)) {
            Fail "Could not read Version for $id in CursorPace.csproj."
        }
        Write-Host "  $id $version"
        if ($null -eq $first) {
            $first = $version
        }
        elseif ($version -ne $first) {
            $mismatched = $true
        }
    }
    if ($mismatched) {
        Write-Host "Error: Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, and Avalonia.Fonts.Inter must share one version." -ForegroundColor Red
        Write-Host "Pin with: .\scripts\update-packages.ps1 Avalonia@$first Avalonia.Desktop@$first Avalonia.Themes.Fluent@$first Avalonia.Fonts.Inter@$first"
        exit 1
    }
    $webview = Get-PackageVersion -Project $AppProject -Id 'Avalonia.Controls.WebView'
    if (-not [string]::IsNullOrWhiteSpace($webview)) {
        Write-Host "  Avalonia.Controls.WebView $webview (may lag the Avalonia line; do not force a version that is not on nuget.org)"
    }
}

if ($Help) {
    Write-Output 'Usage: .\scripts\update-packages.ps1 [options] [package[@version] ...]'
    Write-Output ''
    Write-Output '  With no package list, update every direct PackageReference in the app'
    Write-Output '  and test projects to the highest version on the configured sources.'
    Write-Output '  Named packages are updated only in the project that references them.'
    Write-Output ''
    Write-Output '  -List          List outdated packages and exit (no writes).'
    Write-Output '  -Vulnerable    Lift only packages NuGet Audit reports, to the lowest safe version.'
    Write-Output '  -Test          Run unit tests after the restore.'
    exit 0
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Fail 'dotnet is not on PATH. Install the .NET 10 SDK.'
}

if (-not (Test-Path -LiteralPath $SolutionPath) -or
    -not (Test-Path -LiteralPath $AppProject) -or
    -not (Test-Path -LiteralPath $TestProject)) {
    Fail 'Expected CursorPace.slnx, CursorPace.csproj, and Tests\CursorPace.Tests.csproj in the repository.'
}

if ($List) {
    if ($Packages -and $Packages.Count -gt 0) {
        Fail '-List does not take package names.'
    }
    if ($Test) {
        Fail '-List cannot be combined with -Test.'
    }
    if ($Vulnerable) {
        Write-Host 'Listing vulnerable NuGet packages...'
        Invoke-DotNet -DotNetArgs @('list', $SolutionPath, 'package', '--vulnerable')
        exit 0
    }
    Write-Host 'Listing outdated NuGet packages...'
    Invoke-DotNet -DotNetArgs @('list', $SolutionPath, 'package', '--outdated')
    exit 0
}

$anyUpdated = $false
$appTouched = Update-ProjectPackages -Project $AppProject -Relative 'CursorPace.csproj'
if ($appTouched) {
    $anyUpdated = $true
}
$testsTouched = Update-ProjectPackages -Project $TestProject -Relative 'Tests\CursorPace.Tests.csproj'
if ($testsTouched) {
    $anyUpdated = $true
}

if (-not $anyUpdated) {
    Fail 'None of the named packages are referenced by CursorPace.csproj or Tests\CursorPace.Tests.csproj.'
}

if ($appTouched) {
    Test-AvaloniaAlignment
}

Write-Host 'Restoring lock files...'
Invoke-DotNet -DotNetArgs @('restore', $SolutionPath, '--force-evaluate')

if ($Test) {
    Write-Host 'Running tests...'
    Invoke-DotNet -DotNetArgs @('test', '.\Tests\CursorPace.Tests.csproj')
}

Write-Host 'Commit CursorPace.csproj / Tests\CursorPace.Tests.csproj with packages.lock.json and Tests\packages.lock.json.'
Write-Host 'Log the bump under ## [Unreleased] in dev/CHANGELOG.md.'
