<#
.SYNOPSIS
	Runs the unit tests with code coverage and builds a coverage report for FeBuddy.Core.

.DESCRIPTION
	1. Runs FeBuddy.UnitTests with the coverlet collector (settings in coverlet.runsettings).
	2. Builds an HTML report plus text and GitHub-markdown summaries with ReportGenerator
	   (a local dotnet tool pinned in dotnet-tools.json - no global install needed).
	3. Prints the summary and, when -MinimumLineCoverage / -MinimumBranchCoverage are given,
	   fails if coverage is below them. CI uses this as the "coverage must not drop" gate.

	Everything is written under TestResults\ (gitignored). Open
	TestResults\CoverageReport\index.html to see covered and uncovered lines per file.

.PARAMETER Configuration
	Build configuration for the test run. Defaults to Debug.

.PARAMETER MinimumLineCoverage
	Fail when line coverage (percent) is below this. 0 disables the check.

.PARAMETER MinimumBranchCoverage
	Fail when branch coverage (percent) is below this. 0 disables the check.

.PARAMETER Open
	Open the HTML report when done.

.EXAMPLE
	.\coverage.ps1 -Open
#>
param(
	[string]$Configuration = 'Debug',
	[double]$MinimumLineCoverage = 0,
	[double]$MinimumBranchCoverage = 0,
	[switch]$Open
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$rawResults = Join-Path $root 'TestResults\Coverage'
$reportDir = Join-Path $root 'TestResults\CoverageReport'

foreach ($dir in @($rawResults, $reportDir)) {
	if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
}

Push-Location $root
try {
	dotnet test (Join-Path $root 'FeBuddy.UnitTests\FeBuddy.UnitTests.csproj') `
		-c $Configuration `
		--collect:'XPlat Code Coverage' `
		--settings (Join-Path $root 'coverlet.runsettings') `
		--results-directory $rawResults
	if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

	dotnet tool restore
	if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

	dotnet reportgenerator `
		"-reports:$rawResults\**\coverage.cobertura.xml" `
		"-targetdir:$reportDir" `
		'-reporttypes:HtmlInline;TextSummary;MarkdownSummaryGithub' `
		'-verbosity:Warning'
	if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
	Pop-Location
}

Get-Content (Join-Path $reportDir 'Summary.txt') -TotalCount 20 | Write-Host

# On GitHub Actions, put the summary table on the run's summary page.
if ($env:GITHUB_STEP_SUMMARY) {
	Get-Content (Join-Path $reportDir 'SummaryGithub.md') | Add-Content $env:GITHUB_STEP_SUMMARY
}

$cobertura = Get-ChildItem $rawResults -Recurse -Filter 'coverage.cobertura.xml' | Select-Object -First 1
[xml]$xml = Get-Content $cobertura.FullName
$line = [math]::Round([double]$xml.coverage.'line-rate' * 100, 2)
$branch = [math]::Round([double]$xml.coverage.'branch-rate' * 100, 2)

Write-Host ''
Write-Host "Line coverage:   $line%"
Write-Host "Branch coverage: $branch%"
Write-Host "Report:          $(Join-Path $reportDir 'index.html')"

if ($Open) { Start-Process (Join-Path $reportDir 'index.html') }

$failed = $false
if ($MinimumLineCoverage -gt 0 -and $line -lt $MinimumLineCoverage) {
	Write-Host "Line coverage $line% is below the minimum of $MinimumLineCoverage%." -ForegroundColor Red
	$failed = $true
}
if ($MinimumBranchCoverage -gt 0 -and $branch -lt $MinimumBranchCoverage) {
	Write-Host "Branch coverage $branch% is below the minimum of $MinimumBranchCoverage%." -ForegroundColor Red
	$failed = $true
}
if ($failed) { exit 1 }
