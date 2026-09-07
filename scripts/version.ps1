<#
.SYNOPSIS
  Show or set the Cursor Pace version.

.DESCRIPTION
  With no argument, prints the current version from CursorPace.csproj.
  With a version argument, writes that version to CursorPace.csproj and
  the default MyAppVersion in setup.iss.

.PARAMETER Version
  New version (x.y.z). Omit to print the current version.

.EXAMPLE
  .\scripts\version.ps1

.EXAMPLE
  .\scripts\version.ps1 0.2.4
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Version,

    [switch]$Help
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$CsprojPath = Join-Path $RepoRoot 'CursorPace.csproj'
$IssPath = Join-Path $RepoRoot 'setup.iss'

function Fail {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "Error: $Message" -ForegroundColor Red
    exit 1
}

function Get-Utf8Text {
    param([Parameter(Mandatory = $true)][string]$Path)
    $encoding = New-Object System.Text.UTF8Encoding $false
    return [System.IO.File]::ReadAllText($Path, $encoding)
}

function Set-Utf8Text {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text
    )
    $encoding = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Text, $encoding)
}

function Get-CsprojVersion {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        Fail "CursorPace.csproj not found in repository root."
    }
    $raw = Get-Utf8Text -Path $Path
    if ($raw -notmatch '(?m)^\s*<Version>([^<]+)</Version>\s*$') {
        Fail "Could not find <Version> in CursorPace.csproj"
    }
    $value = $Matches[1].Trim()
    if (-not $value) {
        Fail "Could not read CursorPace.csproj version."
    }
    return $value
}

function Get-IssVersion {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        Fail "setup.iss not found in repository root."
    }
    $raw = Get-Utf8Text -Path $Path
    if ($raw -notmatch '(?m)^#define MyAppVersion "([^"]+)"') {
        Fail "Could not find MyAppVersion in setup.iss"
    }
    return $Matches[1].Trim()
}

function Test-AppVersion {
    param([Parameter(Mandatory = $true)][string]$Value)
    return $Value -match '^[0-9]+\.[0-9]+\.[0-9]+$'
}

function Update-CsprojVersion {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$NewVersion
    )
    $raw = Get-Utf8Text -Path $Path
    $regex = New-Object System.Text.RegularExpressions.Regex '(?m)^(\s*)<Version>[^<]+</Version>(\s*)$'
    $updated = $regex.Replace($raw, "`${1}<Version>$NewVersion</Version>`${2}", 1)
    if ($updated -eq $raw) {
        Fail "Could not update <Version> in CursorPace.csproj"
    }
    Set-Utf8Text -Path $Path -Text $updated
}

function Update-IssVersion {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$NewVersion
    )
    $raw = Get-Utf8Text -Path $Path
    $regex = New-Object System.Text.RegularExpressions.Regex '(?m)^(#define MyAppVersion ")[^"]+(")'
    $updated = $regex.Replace($raw, "`${1}$NewVersion`${2}", 1)
    if ($updated -eq $raw) {
        Fail "Could not update MyAppVersion in setup.iss"
    }
    Set-Utf8Text -Path $Path -Text $updated
}

if ($Help) {
    Write-Output "Usage: .\scripts\version.ps1 [version]"
    Write-Output ""
    Write-Output "  With no argument, print the current version from CursorPace.csproj."
    Write-Output "  With x.y.z, set that version in CursorPace.csproj and setup.iss."
    exit 0
}

$current = Get-CsprojVersion -Path $CsprojPath
$issVersion = Get-IssVersion -Path $IssPath

if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Output $current
    if ($issVersion -ne $current) {
        Write-Host "Warning: setup.iss default MyAppVersion is $issVersion (expected $current)." -ForegroundColor Yellow
    }
    exit 0
}

$requested = $Version.Trim()
if ($requested.StartsWith('v') -or $requested.StartsWith('V')) {
    $requested = $requested.Substring(1)
}

if (-not (Test-AppVersion -Value $requested)) {
    Fail "Version must be x.y.z (digits), for example 0.2.4."
}

if ($current -eq $requested -and $issVersion -eq $requested) {
    Write-Output $requested
    exit 0
}

if ($current -ne $requested) {
    Update-CsprojVersion -Path $CsprojPath -NewVersion $requested
}
if ($issVersion -ne $requested) {
    Update-IssVersion -Path $IssPath -NewVersion $requested
}

Write-Output $requested
if ($current -ne $requested) {
    Write-Host "Updated CursorPace.csproj <Version> $current -> $requested"
}
else {
    Write-Host "CursorPace.csproj already $requested"
}
if ($issVersion -ne $requested) {
    Write-Host "Updated setup.iss MyAppVersion $issVersion -> $requested"
}
else {
    Write-Host "setup.iss already $requested"
}
