<#
.SYNOPSIS
	Shared helpers for FE-BUDDY's release scripts. Dot-source it: . "$PSScriptRoot\ReleaseCommon.ps1"

.DESCRIPTION
	Works in Windows PowerShell 5.1 (a local run) and PowerShell 7 (GitHub Actions). See
	docs/Developers/RELEASING.md.
#>

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$WpfProjectPath = Join-Path $RepoRoot 'FeBuddy\FeBuddy.Wpf\FeBuddy.Wpf.csproj'
$AppXamlCsPath = Join-Path $RepoRoot 'FeBuddy\FeBuddy.Wpf\App.xaml.cs'
$ChangeLogPath = Join-Path $RepoRoot 'ChangeLog.md'
$ReleasesDir = Join-Path $RepoRoot 'FeBuddy\releases'
$MsiName = 'FE-BUDDY-Setup.msi'
$CounterRepoPath = 'FeBuddy/FeBuddy.Installer/installer-version-counter.json'
$ReleaseRepository = if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { 'Nikolai558/FE-BUDDY' }

# The one version anyone bumps: <Version> in FeBuddy.Wpf.csproj.
function Get-ProjectVersion {
	[xml]$project = Get-Content -Path $WpfProjectPath -Raw -Encoding UTF8
	$version = @($project.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ })[0]
	if ([string]::IsNullOrWhiteSpace($version)) {
		throw "Could not read <Version> from $WpfProjectPath."
	}
	return $version.Trim()
}

# ChangeLog.md's "## <name>" sections in file order, each as @{ Name; Body } (Body trimmed, HTML
# comments removed). Text before the first section (the file's introduction) is not returned.
function Get-ChangeLogSections {
	$text = (Get-Content -Path $ChangeLogPath -Raw -Encoding UTF8) -replace '(?s)<!--.*?-->', ''
	$sections = New-Object System.Collections.Generic.List[object]
	$current = $null

	foreach ($line in ($text -split "`r?`n")) {
		if ($line -match '^##\s+(.+?)\s*$') {
			$current = [pscustomobject]@{ Name = $Matches[1]; Lines = (New-Object System.Collections.Generic.List[string]) }
			$sections.Add($current)
		}
		elseif ($null -ne $current) {
			$current.Lines.Add($line)
		}
	}

	foreach ($section in $sections) {
		[pscustomobject]@{ Name = $section.Name; Body = ($section.Lines -join "`n").Trim() }
	}
}

# Runs ReleaseVersion.cs (FeBuddy.Versioning's rules) and returns its key=value output as a hashtable.
function Invoke-ReleaseVersionTool {
	param(
		[Parameter(Mandatory = $true)][string]$Version,
		[string[]]$PublishedTags = @()
	)

	$toolArgs = @($Version) + @($PublishedTags)
	$output = & dotnet run (Join-Path $PSScriptRoot 'ReleaseVersion.cs') -- @toolArgs
	if ($LASTEXITCODE -ne 0) {
		throw "ReleaseVersion.cs failed (exit code $LASTEXITCODE) for version '$Version'."
	}

	$result = @{}
	foreach ($line in $output) {
		if ($line -match '^(\w+)=(.*)$') { $result[$Matches[1]] = $Matches[2] }
	}
	return $result
}

# Writes a step output (GitHub Actions only).
function Set-StepOutput([string]$Name, [string]$Value) {
	if ($env:GITHUB_OUTPUT) { Add-Content -Path $env:GITHUB_OUTPUT -Value "$Name=$Value" -Encoding utf8 }
}

# Appends Markdown to the run's summary page (GitHub Actions only).
function Add-StepSummary([string[]]$Lines) {
	if ($env:GITHUB_STEP_SUMMARY) { Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $Lines -Encoding utf8 }
}

# Collects pass/fail results so one run reports every problem, not just the first.
$script:CheckResults = New-Object System.Collections.Generic.List[object]

function Add-CheckResult {
	param(
		[Parameter(Mandatory = $true)][string]$Check,
		[Parameter(Mandatory = $true)][ValidateSet('Pass', 'Fail', 'Skip')][string]$Outcome,
		[string]$Detail = ''
	)

	$script:CheckResults.Add([pscustomobject]@{ Check = $Check; Outcome = $Outcome; Detail = $Detail })

	switch ($Outcome) {
		'Pass' { Write-Host "[PASS] $Check$(if ($Detail) { " - $Detail" })" }
		'Skip' { Write-Host "[SKIP] $Check$(if ($Detail) { " - $Detail" })" }
		'Fail' {
			if ($env:GITHUB_ACTIONS -eq 'true') {
				# Workflow-command escaping: % first, then the characters that end a property or line.
				$title = $Check -replace '%', '%25' -replace ':', '%3A' -replace ',', '%2C'
				$message = $Detail -replace '%', '%25' -replace "`r", '%0D' -replace "`n", '%0A'
				Write-Host "::error title=${title}::$message"
			}
			else { Write-Host "[FAIL] $Check - $Detail" -ForegroundColor Red }
		}
	}
}

# Writes the collected results as a summary table and returns $true when nothing failed.
function Complete-Checks([string]$Title) {
	$icons = @{ Pass = ':white_check_mark:'; Fail = ':x:'; Skip = ':heavy_minus_sign:' }
	$summary = @("### $Title", '', '| | Check | Detail |', '|---|---|---|')
	foreach ($result in $script:CheckResults) {
		$detail = $result.Detail -replace '\|', '\|'
		$summary += "| $($icons[$result.Outcome]) | $($result.Check) | $detail |"
	}
	Add-StepSummary ($summary + '')

	$failed = @($script:CheckResults | Where-Object { $_.Outcome -eq 'Fail' }).Count
	if ($failed -gt 0) {
		Write-Host ''
		Write-Host "$failed check(s) failed." -ForegroundColor Red
		return $false
	}
	return $true
}
