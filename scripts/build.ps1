<#
.SYNOPSIS
  Publish a self-contained Release build and compile the Inno Setup installer.

.PARAMETER SkipTests
  Skip unit tests.

.PARAMETER SkipInstaller
  Publish only; do not run Inno Setup.
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$appCsproj = Join-Path $RepoRoot 'CursorPace.csproj'
$issScript = Join-Path $RepoRoot 'setup.iss'
$rid = 'win-x64'

function Get-CsprojProperty {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $raw = Get-Content -LiteralPath $Path -Raw
    $pattern = "<$Name>([^<]+)</$Name>"
    if ($raw -notmatch $pattern) {
        throw "Could not find <$Name> in $Path"
    }
    return $Matches[1].Trim()
}

function Find-ISCC {
    $candidates = New-Object System.Collections.Generic.List[string]

    $fromPath = Get-Command iscc -ErrorAction SilentlyContinue
    if ($fromPath) {
        $candidates.Add($fromPath.Source)
    }

    $candidates.Add((Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'))
    $candidates.Add((Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'))
    $candidates.Add((Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'))

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    throw "Inno Setup 6 was not found. Install it from https://jrsoftware.org/isdl.php then re-run this script."
}

$tfm = Get-CsprojProperty -Path $appCsproj -Name 'TargetFramework'
$version = Get-CsprojProperty -Path $appCsproj -Name 'Version'
$publishDir = Join-Path $RepoRoot "bin\Release\$tfm\$rid\publish"
$publishDirRelative = "bin\Release\$tfm\$rid\publish"

Write-Host "Publishing $version ($tfm / $rid, self-contained)..."
if (-not $SkipTests) {
    Write-Host "Running tests..."
    dotnet test .\Tests\CursorPace.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

dotnet publish $appCsproj -c Release -r $rid --self-contained `
    -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$publishedExe = Join-Path $publishDir 'CursorPace.exe'
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Publish succeeded but did not produce $publishedExe"
}

Write-Host "Published: $publishDir"

if ($SkipInstaller) {
    Write-Host "Skipping installer (-SkipInstaller)."
    exit 0
}

$iscc = Find-ISCC
Write-Host "Compiling installer with $iscc ..."
& $iscc "/DMyAppVersion=$version" "/DPublishDir=$publishDirRelative" $issScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE"
}

$installerName = "CursorPace-$version-win-x64-setup.exe"
$installerPath = Join-Path $RepoRoot "installer\$installerName"
if (-not (Test-Path -LiteralPath $installerPath)) {
    throw "Inno Setup finished but $installerPath was not created"
}

$hash = Get-FileHash -LiteralPath $installerPath -Algorithm SHA256
$hashPath = "$installerPath.sha256"
"$($hash.Hash)  $installerName" | Set-Content -LiteralPath $hashPath -Encoding ascii

Write-Host "Installer: $installerPath"
Write-Host "SHA256:    $($hash.Hash)"
Write-Host "Checksum:  $hashPath"
