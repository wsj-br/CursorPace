<#
.SYNOPSIS
  Create a GitHub release from HEAD using the csproj version.

.DESCRIPTION
  Reads <Version> from CursorUsageProgress.csproj and requires
  release-notes/RELEASE_NOTES_<version>.md. Then:

  - Deletes an existing GitHub release and/or tag for v<version> if present
  - Creates an annotated tag at HEAD and pushes it to origin
  - Creates the GitHub release (the Release workflow builds and attaches the installer)

  If the tag or a GitHub release for it already exists, they are removed and the
  tag is recreated at the current HEAD so you can fix a mistaken tag or re-run
  after new commits.

.PARAMETER DryRun
  Validate and print planned steps; no deletes, tag, push, or release.

.PARAMETER VerifyClean
  Require a clean git working tree. Default: $true. Pass -VerifyClean:$false to skip.

.EXAMPLE
  .\scripts\release.ps1

.EXAMPLE
  .\scripts\release.ps1 -DryRun

.EXAMPLE
  .\scripts\release.ps1 -VerifyClean:$false
#>
[CmdletBinding()]
param(
    [switch]$DryRun,

    [bool]$VerifyClean = $true
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

function Fail {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "Error: $Message" -ForegroundColor Red
    exit 1
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [switch]$Inherit,
        [switch]$AllowFail
    )

    if ($Inherit) {
        & $FilePath @ArgumentList
        $code = $LASTEXITCODE
        if ($null -eq $code) {
            $code = 0
        }
        if ($code -ne 0 -and -not $AllowFail) {
            exit $code
        }
        return [pscustomobject]@{ Status = $code; Stdout = ''; Stderr = '' }
    }

    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = & $FilePath @ArgumentList 2>&1
    }
    finally {
        $ErrorActionPreference = $prevEap
    }

    $code = $LASTEXITCODE
    if ($null -eq $code) {
        $code = 0
    }

    $text = @(
        foreach ($line in @($lines)) {
            if ($null -ne $line) {
                [string]$line
            }
        }
    ) -join [Environment]::NewLine

    if ($code -ne 0 -and -not $AllowFail) {
        $detail = $text.Trim()
        if ($detail) {
            Write-Host $detail
        }
        exit $code
    }

    return [pscustomobject]@{ Status = $code; Stdout = $text; Stderr = '' }
}

function Test-RequiredCommand {
    param([Parameter(Mandatory = $true)][string]$Name)
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) {
        Fail "Missing required command: $Name"
    }
    $probe = Invoke-Native -FilePath $Name -ArgumentList @('--version') -AllowFail
    if ($probe.Status -ne 0) {
        Fail "Missing required command: $Name"
    }
}

function Get-CsprojVersion {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        Fail "CursorUsageProgress.csproj not found in repository root."
    }
    $raw = Get-Content -LiteralPath $Path -Raw
    if ($raw -notmatch '<Version>([^<]+)</Version>') {
        Fail "Could not find <Version> in CursorUsageProgress.csproj"
    }
    $value = $Matches[1].Trim()
    if (-not $value) {
        Fail "Could not read CursorUsageProgress.csproj version."
    }
    return $value
}

function ConvertTo-RepoWebUrl {
    param([Parameter(Mandatory = $true)][string]$RemoteUrl)
    $url = $RemoteUrl.Trim()
    if ($url -match '^git@github\.com:(.+?)(?:\.git)?$') {
        return "https://github.com/$($Matches[1])"
    }
    if ($url -match '^https://github\.com/(.+?)(?:\.git)?$') {
        return "https://github.com/$($Matches[1])"
    }
    if ($url -match '^ssh://git@github\.com/(.+?)(?:\.git)?$') {
        return "https://github.com/$($Matches[1])"
    }
    return $url
}

function Format-ArgForDisplay {
    param([Parameter(Mandatory = $true)][string]$Value)
    if ($Value -match '\s') {
        return ('"{0}"' -f $Value)
    }
    return $Value
}

Test-RequiredCommand -Name git
Test-RequiredCommand -Name gh

$inside = Invoke-Native -FilePath git -ArgumentList @('rev-parse', '--is-inside-work-tree') -AllowFail
if ($inside.Status -ne 0 -or $inside.Stdout.Trim() -ne 'true') {
    Fail 'Not inside a git repository.'
}

$auth = Invoke-Native -FilePath gh -ArgumentList @('auth', 'status') -AllowFail
if ($auth.Status -ne 0) {
    Fail 'GitHub CLI is not authenticated. Run: gh auth login'
}

$version = Get-CsprojVersion -Path (Join-Path $RepoRoot 'CursorUsageProgress.csproj')
$tag = "v$version"
$notesFile = "release-notes/RELEASE_NOTES_$version.md"
$notesPath = Join-Path $RepoRoot $notesFile

if (-not (Test-Path -LiteralPath $notesPath)) {
    Fail "Release notes file not found: $notesFile"
}

if ($VerifyClean) {
    $status = Invoke-Native -FilePath git -ArgumentList @('status', '--porcelain')
    if ($status.Stdout.Trim()) {
        Fail 'Working tree is not clean. Commit/stash changes or run with -VerifyClean:$false'
    }
}

$remote = Invoke-Native -FilePath git -ArgumentList @('remote', 'get-url', 'origin') -AllowFail
if ($remote.Status -ne 0) {
    Fail "Remote 'origin' not configured."
}
$repoUrl = ConvertTo-RepoWebUrl -RemoteUrl $remote.Stdout

$headCommit = (Invoke-Native -FilePath git -ArgumentList @('rev-parse', 'HEAD')).Stdout.Trim()

function Test-RemoteTagExists {
    $result = Invoke-Native -FilePath git -ArgumentList @('ls-remote', 'origin', "refs/tags/$tag") -AllowFail
    return ($result.Status -eq 0) -and ($result.Stdout.Trim().Length -gt 0)
}

function Test-LocalTagExists {
    $result = Invoke-Native -FilePath git -ArgumentList @('rev-parse', '-q', '--verify', "refs/tags/$tag") -AllowFail
    return $result.Status -eq 0
}

function Test-ReleaseExists {
    $result = Invoke-Native -FilePath gh -ArgumentList @('release', 'view', $tag) -AllowFail
    return $result.Status -eq 0
}

function Set-ReleaseTagAtHead {
    if ($DryRun) {
        Write-Host "[dry-run] HEAD commit: $headCommit"
        if (Test-ReleaseExists) {
            Write-Host "[dry-run] Would delete GitHub release: $tag"
        }
        if (Test-RemoteTagExists) {
            Write-Host "[dry-run] Would delete remote tag: origin $tag"
        }
        if (Test-LocalTagExists) {
            Write-Host "[dry-run] Would delete local tag: $tag"
        }
        Write-Host "[dry-run] Would create annotated tag $tag at HEAD and push to origin."
        return
    }

    if (Test-ReleaseExists) {
        Write-Host "Deleting existing GitHub release $tag (and its tag on the remote)..."
        Invoke-Native -FilePath gh -ArgumentList @('release', 'delete', $tag, '--yes', '--cleanup-tag') -Inherit
    }
    elseif (Test-RemoteTagExists) {
        Write-Host "Deleting remote tag $tag..."
        Invoke-Native -FilePath git -ArgumentList @('push', 'origin', ":refs/tags/$tag") -Inherit
    }

    if (Test-LocalTagExists) {
        Write-Host "Deleting local tag $tag..."
        Invoke-Native -FilePath git -ArgumentList @('tag', '-d', $tag) -Inherit
    }

    Write-Host "Creating annotated tag $tag at HEAD ($headCommit)..."
    Invoke-Native -FilePath git -ArgumentList @('tag', '-a', $tag, '-m', "Release $tag", 'HEAD') -Inherit

    Write-Host "Pushing tag $tag to origin..."
    Invoke-Native -FilePath git -ArgumentList @('push', 'origin', "refs/tags/$tag") -Inherit
}

Set-ReleaseTagAtHead

$createArgs = @('release', 'create', $tag, '--title', $tag, '--notes-file', $notesFile)

Write-Host 'Release inputs:'
Write-Host "  Tag:        $tag"
Write-Host "  Title:      $tag"
Write-Host "  Notes file: $notesFile"

if ($DryRun) {
    $display = ($createArgs | ForEach-Object { Format-ArgForDisplay -Value $_ }) -join ' '
    Write-Host '[dry-run] Would run:'
    Write-Host "  gh $display"
    exit 0
}

Invoke-Native -FilePath gh -ArgumentList $createArgs -Inherit
Write-Host "Release created successfully: $tag"
Write-Host ''
Write-Host "The Release workflow will build the installer and attach it to $tag."
Write-Host "See progress at $repoUrl"
Write-Host ''
