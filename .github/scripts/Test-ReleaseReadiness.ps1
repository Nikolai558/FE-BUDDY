<#
.SYNOPSIS
	FE-BUDDY's release pre-flight: checks that the version in FeBuddy.Wpf.csproj can be released.

.DESCRIPTION
	Runs every check that does not need a build, and reports every failure at once:

	  - <Version> is a release version: X.Y.Z or X.Y.Z-alpha.N / -beta.N / -rc.N (never -dev).
	  - It is exactly one step after the latest published release (see FeBuddy.Versioning's
	    ReleaseStep: 3.0.0-alpha.1 -> alpha.2, beta.1, rc.1 or 3.0.0 - not alpha.4).
	  - No tag, release or draft release already uses it.
	  - ChangeLog.md has a non-empty "## <version>" section, it is the newest one, and
	    "## Unreleased" is empty.
	  - Developer mode is off (App.DevModeEnabled = false).
	  - The token the release workflow commits the installer counter with works, can push, and is
	    not about to expire (only when RELEASE_COUNTER_TOKEN is set - i.e. on GitHub Actions).

	Build-ReleaseAssets.ps1 does the rest (build, MSI checks, release notes). The "Release checks"
	workflow runs both on every pull request into `releases`; the "Release" workflow runs them again
	before drafting. Run it yourself before opening that pull request:

	    powershell -File .github\scripts\Test-ReleaseReadiness.ps1

	Needs the .NET 10 SDK and the GitHub CLI (gh), signed in. See docs/Developers/RELEASING.md.

.PARAMETER SkipVersionStep
	Do not require the one-step rule (still reported, as a warning). For a deliberate exception;
	on a pull request, add the "skip-version-step" label instead.
#>
param(
	[switch]$SkipVersionStep
)

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

# ------------------------------------------------------------------ version

$version = Get-ProjectVersion
Write-Host "Version (FeBuddy.Wpf.csproj): $version"
Write-Host ''

# ------------------------------------------------------------------ GitHub: releases and tags

$releaseRows = gh api "repos/$ReleaseRepository/releases?per_page=100" --paginate --jq '.[] | [.tag_name, (.draft | tostring)] | @tsv'
if ($LASTEXITCODE -ne 0) { throw "Could not list the releases of $ReleaseRepository (is gh signed in / GH_TOKEN set?)." }

$releases = @($releaseRows | Where-Object { $_ } | ForEach-Object {
		$parts = $_ -split "`t"
		[pscustomobject]@{ Tag = $parts[0]; Draft = ($parts[1] -eq 'true') }
	})
$publishedTags = @($releases | Where-Object { -not $_.Draft } | ForEach-Object { $_.Tag })

$tool = Invoke-ReleaseVersionTool -Version $version -PublishedTags $publishedTags
$previous = $tool['previous']

# ------------------------------------------------------------------ checks

if ($tool['releaseShaped'] -eq 'true') {
	Add-CheckResult 'Release version' 'Pass' "$version ($($tool['channel']))"
}
else {
	Add-CheckResult 'Release version' 'Fail' ("<Version> in FeBuddy.Wpf.csproj is '$version'. A release must be X.Y.Z or " +
		'X.Y.Z-alpha.N / -beta.N / -rc.N (lowercase, N from 1, no -dev or +metadata).')
}

if ($tool['nextStep'] -eq 'true') {
	Add-CheckResult 'One step after the last release' 'Pass' "$previous -> $version"
}
elseif ($SkipVersionStep) {
	Add-CheckResult 'One step after the last release' 'Skip' "$previous -> $version is not one step, but the check was skipped on purpose."
}
else {
	Add-CheckResult 'One step after the last release' 'Fail' ("The latest release is $previous, so the next one must be one of: " +
		"$($tool['allowed']). <Version> is $version.")
}

$sameRelease = @($releases | Where-Object { $_.Tag -eq $version })
if ($sameRelease.Count -gt 0) {
	$kind = if ($sameRelease[0].Draft) { 'A draft release' } else { 'A release' }
	Add-CheckResult 'Release does not exist yet' 'Fail' "$kind for $version already exists. Publish or delete it, or bump <Version>."
}
else {
	$tagRefs = gh api "repos/$ReleaseRepository/git/matching-refs/tags/$version" --jq '.[].ref'
	if ($LASTEXITCODE -ne 0) { throw "Could not look up tag $version." }
	if (@($tagRefs) -contains "refs/tags/$version") {
		Add-CheckResult 'Release does not exist yet' 'Fail' "Tag $version already exists. Bump <Version>."
	}
	else {
		Add-CheckResult 'Release does not exist yet' 'Pass' "No tag or release named $version."
	}
}

# ChangeLog.md
$sections = @(Get-ChangeLogSections)
$unreleased = @($sections | Where-Object { $_.Name -eq 'Unreleased' })
$versionSections = @($sections | Where-Object { $_.Name -ne 'Unreleased' })

if ($unreleased.Count -gt 0 -and $unreleased[0].Body) {
	Add-CheckResult 'ChangeLog.md: Unreleased is empty' 'Fail' ("'## Unreleased' still has entries. Rename it to '## $version' and " +
		"add a new, empty '## Unreleased' above it.")
}
else {
	Add-CheckResult 'ChangeLog.md: Unreleased is empty' 'Pass'
}

$ownSection = @($versionSections | Where-Object { $_.Name -eq $version })
if ($ownSection.Count -eq 0) {
	Add-CheckResult "ChangeLog.md: '## $version' section" 'Fail' "ChangeLog.md has no '## $version' section."
}
elseif ($versionSections[0].Name -ne $version) {
	Add-CheckResult "ChangeLog.md: '## $version' section" 'Fail' "'## $version' is not the newest section - '## $($versionSections[0].Name)' is above it."
}
elseif (-not $ownSection[0].Body) {
	Add-CheckResult "ChangeLog.md: '## $version' section" 'Fail' "'## $version' has no entries."
}
else {
	Add-CheckResult "ChangeLog.md: '## $version' section" 'Pass' "$(@($ownSection[0].Body -split "`n" | Where-Object { $_ -match '^\s*[-*] ' }).Count) bullet(s)."
}

# Developer mode
$appXaml = Get-Content -Path $AppXamlCsPath -Raw -Encoding UTF8
if ($appXaml -match 'private\s+const\s+bool\s+DevModeEnabled\s*=\s*false\s*;') {
	Add-CheckResult 'Developer mode is off' 'Pass'
}
else {
	Add-CheckResult 'Developer mode is off' 'Fail' 'App.DevModeEnabled (FeBuddy.Wpf\App.xaml.cs) must be false in a release.'
}

# The installer-counter token (the release workflow commits the counter to v3-development with it)
if ($env:RELEASE_COUNTER_TOKEN) {
	$savedToken = $env:GH_TOKEN
	$env:GH_TOKEN = $env:RELEASE_COUNTER_TOKEN
	try {
		$headers = gh api -i user
		$tokenWorks = ($LASTEXITCODE -eq 0)
		$canPush = if ($tokenWorks) { gh api "repos/$ReleaseRepository" --jq '.permissions.push' } else { 'false' }
	}
	finally {
		$env:GH_TOKEN = $savedToken
	}

	$expiryLine = @($headers | Where-Object { $_ -match '^github-authentication-token-expiration:\s*(.+)$' })[0]
	$expires = $null
	if ($expiryLine -and ($expiryLine -replace ' UTC\s*$', ' +0000') -match '(\d{4}-\d{2}-\d{2}) (\d{2}:\d{2}:\d{2}) ([+-]\d{2})(\d{2})') {
		$expires = [DateTimeOffset]::Parse("$($Matches[1])T$($Matches[2])$($Matches[3]):$($Matches[4])", [Globalization.CultureInfo]::InvariantCulture)
	}

	if (-not $tokenWorks) {
		Add-CheckResult 'Installer-counter token' 'Fail' 'The RELEASE_COUNTER_PAT secret does not work (expired or revoked?). Make a new token and update the secret.'
	}
	elseif ($canPush -ne 'true') {
		Add-CheckResult 'Installer-counter token' 'Fail' "The RELEASE_COUNTER_PAT secret's account cannot push to $ReleaseRepository."
	}
	elseif ($expires -and $expires -lt [DateTimeOffset]::UtcNow.AddDays(7)) {
		Add-CheckResult 'Installer-counter token' 'Fail' "The RELEASE_COUNTER_PAT secret expires $($expires.ToString('yyyy-MM-dd')). Renew it before releasing."
	}
	else {
		$expiryText = if ($expires) { "expires $($expires.ToString('yyyy-MM-dd'))" } else { 'no expiry' }
		Add-CheckResult 'Installer-counter token' 'Pass' $expiryText
	}
}
elseif ($env:GITHUB_ACTIONS -eq 'true') {
	Add-CheckResult 'Installer-counter token' 'Fail' 'The RELEASE_COUNTER_PAT secret is not set.'
}
else {
	Add-CheckResult 'Installer-counter token' 'Skip' 'Checked on GitHub Actions only.'
}

# ------------------------------------------------------------------ result

Set-StepOutput 'version' $version
Set-StepOutput 'previous' $previous
Set-StepOutput 'prerelease' $tool['prerelease']

Write-Host ''
if (-not (Complete-Checks "Release pre-flight: $version")) { exit 1 }
Write-Host "Pre-flight passed for $version."
