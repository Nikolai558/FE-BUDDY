<#
.SYNOPSIS
    Computes the MSI-safe internal ProductVersion for a given real FE-BUDDY version, and
    advances the persisted release counter. Owned entirely by build.ps1 - nobody should
    hand-edit installer-version-counter.json or need to track this by hand.

    See ..\docs\MSI-VERSION-NUMBERING.md for the full explanation of why this exists.

.DESCRIPTION
    Windows Installer's ProductVersion is strictly numeric (major.minor.build, no
    pre-release labels), so it cannot hold the real semantic version (e.g.
    "2.8.4-alpha.1"). This script derives a disposable, order-agnostic internal number:

      - major always equals the real version's major (so Settings > Apps at least shows
        the correct major line, even though it can't show the full real version).
      - minor.build together are one continuously-incrementing counter, bumped by exactly
        1 for every single release this script ever produces (alpha, beta, rc, or stable -
        it does not matter which), and reset to 0.0 the moment major changes.

    This number carries no ordering meaning by itself; MajorUpgrade/AllowDowngrades="yes"
    means WiX does not rely on it for upgrade/downgrade decisions - EnforceVersionPolicy
    (FE-BUDDY.Installer.CustomActions) does that using the real semantic version instead.
    This counter's only job is to be different from whatever's currently installed, so
    Windows Installer agrees to replace files.

.PARAMETER RealVersion
    The real FE-BUDDY semantic version being built, e.g. "2.8.4-alpha.1" or "2.8.4".

.PARAMETER CounterFilePath
    Path to the persisted counter state (installer-version-counter.json).

.OUTPUTS
    The computed "major.minor.build" string to pass as -p:InstallerVersion.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$RealVersion,

    [Parameter(Mandatory = $true)]
    [string]$CounterFilePath
)

$ErrorActionPreference = "Stop"

if ($RealVersion -notmatch '^(?<major>\d+)\.') {
    throw "Get-InstallerVersion: could not parse a major version number from '$RealVersion'."
}
$realMajor = [int]$Matches['major']

$counter = $null
if (Test-Path $CounterFilePath) {
    $counter = Get-Content -Path $CounterFilePath -Raw | ConvertFrom-Json
}

if ($null -eq $counter -or [int]$counter.major -ne $realMajor) {
    # First release ever, or the major version just changed - start this major's counter fresh.
    $counter = [PSCustomObject]@{
        major        = $realMajor
        counterMinor = 0
        counterBuild = 0
    }
}
else {
    $nextBuild = [int]$counter.counterBuild + 1
    $nextMinor = [int]$counter.counterMinor

    if ($nextBuild -gt 65535) {
        $nextMinor += 1
        $nextBuild = 0
    }
    if ($nextMinor -gt 255) {
        throw ("Get-InstallerVersion: counter exhausted for major version $realMajor " +
               "(minor.build ran past 255.65535 - this should never realistically happen; " +
               "see docs/MSI-VERSION-NUMBERING.md).")
    }

    $counter.counterMinor = $nextMinor
    $counter.counterBuild = $nextBuild
}

($counter | ConvertTo-Json) | Set-Content -Path $CounterFilePath -Encoding utf8

Write-Output "$($counter.major).$($counter.counterMinor).$($counter.counterBuild)"
