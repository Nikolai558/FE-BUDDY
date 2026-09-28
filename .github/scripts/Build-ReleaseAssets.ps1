<#
.SYNOPSIS
	Builds FE-BUDDY's release MSI, checks it, and writes the release notes.

.DESCRIPTION
	The half of the release pre-flight that needs a build (Test-ReleaseReadiness.ps1 is the other):

	  - Runs FeBuddy\build.ps1.
	  - The built FE-BUDDY.dll's Product version equals <Version> in FeBuddy.Wpf.csproj.
	  - FeBuddy\releases\FE-BUDDY-Setup.msi exists, and its ProductSemVer is that version, and its
	    UpgradeCode, ProductName and Manufacturer are still FE-BUDDY 2.x's. If they changed, the MSI
	    would install beside 2.x instead of upgrading it (docs/Developers/VERSIONING.md).
	  - Fills in .github\release-notes-template.md with the version's ChangeLog.md section, the
	    download link and the MSI's SHA-256, into FeBuddy\releases\release-notes.md.

	On GitHub Actions the notes also go on the run's summary page, so a pull request into
	`releases` shows exactly what its release will say. Run it yourself to preview them:

	    powershell -File .github\scripts\Build-ReleaseAssets.ps1

	See docs/Developers/RELEASING.md.

.PARAMETER PreviousVersion
	The latest published release, for the notes' "Every change since" link. Blank leaves it out.
	Test-ReleaseReadiness.ps1 outputs it as "previous".

.PARAMETER SkipBuild
	Check and write notes for the MSI already in FeBuddy\releases instead of building a new one.
#>
param(
	[string]$PreviousVersion = '',
	[switch]$SkipBuild
)

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

# FE-BUDDY 2.x's MSI identity. The 3.x MSI must keep it to upgrade 2.x in place - never change these.
$expectedUpgradeCode = '{48F80EB0-2BA6-45F3-A0B5-96E9A05CF2B0}'
$expectedProductName = 'FE-BUDDY'

$version = Get-ProjectVersion
$msiPath = Join-Path $ReleasesDir $MsiName
$notesPath = Join-Path $ReleasesDir 'release-notes.md'

# Reads an MSI's Property table through the Windows Installer COM API.
function Get-MsiProperties([string]$Path) {
	$installer = New-Object -ComObject WindowsInstaller.Installer
	$database = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @($Path, 0))
	$view = $database.GetType().InvokeMember('OpenView', 'InvokeMethod', $null, $database, @('SELECT `Property`, `Value` FROM `Property`'))
	$properties = @{}
	try {
		[void]$view.GetType().InvokeMember('Execute', 'InvokeMethod', $null, $view, $null)
		while ($record = $view.GetType().InvokeMember('Fetch', 'InvokeMethod', $null, $view, $null)) {
			$name = $record.GetType().InvokeMember('StringData', 'GetProperty', $null, $record, 1)
			$properties[$name] = $record.GetType().InvokeMember('StringData', 'GetProperty', $null, $record, 2)
		}
	}
	finally {
		[void]$view.GetType().InvokeMember('Close', 'InvokeMethod', $null, $view, $null)
		foreach ($comObject in @($view, $database, $installer)) {
			[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($comObject)
		}
	}
	return $properties
}

# Keeps (without its markers) or removes an "IF name" ... "END IF name" block of the template.
function Set-TemplateBlock([string]$Text, [string]$Name, [bool]$Keep) {
	$pattern = "(?s)<!--\s*IF $Name\s*-->\n?(.*?)<!--\s*END IF $Name\s*-->\n?"
	$replacement = if ($Keep) { '$1' } else { '' }
	return [regex]::Replace($Text, $pattern, $replacement)
}

# ------------------------------------------------------------------ build

if ($SkipBuild) {
	Add-CheckResult 'Build' 'Skip' 'Using the MSI already in FeBuddy\releases.'
}
else {
	# build.ps1 advances the installer counter. Only the release workflow may commit that (to
	# v3-development), so a local run puts the file back afterwards.
	$counterPath = Join-Path $RepoRoot $CounterRepoPath
	$counterBefore = [IO.File]::ReadAllBytes($counterPath)
	try {
		& (Join-Path $RepoRoot 'FeBuddy\build.ps1')
		Add-CheckResult 'Build' 'Pass' 'build.ps1 published the app and built the MSI.'
	}
	catch {
		Add-CheckResult 'Build' 'Fail' "build.ps1 failed: $($_.Exception.Message)"
		[void](Complete-Checks "Release build: $version")
		exit 1
	}
	finally {
		if ($env:GITHUB_ACTIONS -ne 'true') { [IO.File]::WriteAllBytes($counterPath, $counterBefore) }
	}
}

# ------------------------------------------------------------------ built app and MSI

$dllPath = Join-Path $RepoRoot 'FeBuddy\publish\FE-BUDDY.dll'
if (Test-Path $dllPath) {
	$dllVersion = "$((Get-Item $dllPath).VersionInfo.ProductVersion)".Trim()
	if ($dllVersion -eq $version) {
		Add-CheckResult 'Built app version' 'Pass' "FE-BUDDY.dll is $dllVersion."
	}
	else {
		Add-CheckResult 'Built app version' 'Fail' "FeBuddy.Wpf.csproj says $version but the built FE-BUDDY.dll says '$dllVersion'."
	}
}
else {
	Add-CheckResult 'Built app version' 'Fail' 'FeBuddy\publish\FE-BUDDY.dll was not built.'
}

$sha256 = ''
if (Test-Path $msiPath) {
	$sha256 = (Get-FileHash -Path $msiPath -Algorithm SHA256).Hash.ToLowerInvariant()
	$properties = Get-MsiProperties $msiPath
	$problems = @()

	if ($properties['PRODUCTSEMVER'] -ne $version) {
		$problems += "ProductSemVer is '$($properties['PRODUCTSEMVER'])', not $version"
	}
	if ($properties['UpgradeCode'] -ne $expectedUpgradeCode) {
		$problems += "UpgradeCode is '$($properties['UpgradeCode'])', not 2.x's $expectedUpgradeCode"
	}
	if ($properties['ProductName'] -cne $expectedProductName) {
		$problems += "ProductName is '$($properties['ProductName'])', not $expectedProductName"
	}
	if ($properties['Manufacturer'] -cne $expectedProductName) {
		$problems += "Manufacturer is '$($properties['Manufacturer'])', not $expectedProductName"
	}

	if ($problems.Count -eq 0) {
		Add-CheckResult 'MSI' 'Pass' "$MsiName - ProductSemVer $version, internal version $($properties['ProductVersion']), 2.x UpgradeCode."
	}
	else {
		Add-CheckResult 'MSI' 'Fail' (($problems -join '; ') + '.')
	}
}
else {
	Add-CheckResult 'MSI' 'Fail' "FeBuddy\releases\$MsiName was not built."
}

# ------------------------------------------------------------------ release notes

$tool = Invoke-ReleaseVersionTool -Version $version
$section = @(Get-ChangeLogSections | Where-Object { $_.Name -eq $version })

if ($section.Count -eq 0 -or -not $section[0].Body) {
	Add-CheckResult 'Release notes' 'Fail' "ChangeLog.md has no entries under '## $version'."
}
else {
	$notes = (Get-Content -Path (Join-Path $RepoRoot '.github\release-notes-template.md') -Raw -Encoding UTF8) -replace "`r`n", "`n"
	$notes = Set-TemplateBlock $notes 'PRERELEASE' ($tool['prerelease'] -eq 'true')
	$notes = Set-TemplateBlock $notes 'PREVIOUS' (-not [string]::IsNullOrWhiteSpace($PreviousVersion))

	$tokens = [ordered]@{
		'{{VERSION}}'          = $version
		'{{CHANNEL}}'          = $tool['channel']
		'{{MSI_NAME}}'         = $MsiName
		'{{MSI_URL}}'          = "https://github.com/$ReleaseRepository/releases/download/$version/$MsiName"
		'{{PREVIOUS_VERSION}}' = $PreviousVersion
		'{{COMPARE_URL}}'      = "https://github.com/$ReleaseRepository/compare/$PreviousVersion...$version"
		'{{SHA256}}'           = $sha256
		'{{CHANGELOG}}'        = $section[0].Body
	}
	foreach ($token in $tokens.Keys) {
		$notes = $notes.Replace($token, $tokens[$token])
	}
	$notes = ($notes -replace '(?s)<!--.*?-->\n?', '').Trim() + "`n"

	$leftover = [regex]::Matches($notes, '\{\{[A-Z_]+\}\}') | ForEach-Object { $_.Value } | Select-Object -Unique
	if ($leftover) {
		Add-CheckResult 'Release notes' 'Fail' "The template has unknown tokens: $($leftover -join ', ')."
	}
	elseif (-not $sha256) {
		Add-CheckResult 'Release notes' 'Fail' 'No MSI to take the SHA-256 of.'
	}
	else {
		New-Item -ItemType Directory -Path $ReleasesDir -Force | Out-Null
		[IO.File]::WriteAllText($notesPath, $notes, (New-Object Text.UTF8Encoding $false))
		Add-CheckResult 'Release notes' 'Pass' "Written to FeBuddy\releases\release-notes.md."
		$notesPreview = @("## Release notes preview: v$version", '', '> This is what the drafted release (and FE-BUDDY''s update window) will show.', '', '---', '', $notes)
	}
}

# ------------------------------------------------------------------ result

Set-StepOutput 'msi' $msiPath
Set-StepOutput 'notes' $notesPath
Set-StepOutput 'sha256' $sha256

Write-Host ''
$passed = Complete-Checks "Release build: $version"
if ($notesPreview) { Add-StepSummary $notesPreview }
if (-not $passed) { exit 1 }
Write-Host "Release assets ready: $msiPath"
Write-Host "Release notes:        $notesPath"
