<#
.SYNOPSIS
	Commits the installer counter that build.ps1 advanced to v3-development. Release workflow only.

.DESCRIPTION
	build.ps1 advances FeBuddy\FeBuddy.Installer\installer-version-counter.json on every build (see
	docs/Developers/MSI-VERSION-NUMBERING.md). After a release build, this commits the advanced file
	to v3-development - and only there: `releases` receives it with the next release's pull request,
	so the next release builds with the next number.

	The commit goes through GitHub's GraphQL createCommitOnBranch mutation, not git push: GitHub
	creates and signs it, so it shows as Verified. GH_TOKEN must be a token whose account may push
	to v3-development past its ruleset (the RELEASE_COUNTER_PAT secret).

.PARAMETER Release
	The release being made, for the commit message.

.PARAMETER Branch
	The branch to commit to.
#>
param(
	[Parameter(Mandatory = $true)][string]$Release,
	[string]$Branch = 'v3-development'
)

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

# The Windows checkout may have CRLF; the repository stores LF.
$bumped = (Get-Content -Path (Join-Path $RepoRoot $CounterRepoPath) -Raw -Encoding UTF8) -replace "`r`n", "`n"
$counter = $bumped | ConvertFrom-Json
$counterVersion = "$($counter.major).$($counter.counterMinor).$($counter.counterBuild)"

$current = gh api "repos/$ReleaseRepository/contents/$($CounterRepoPath)?ref=$Branch" -H 'Accept: application/vnd.github.raw'
if ($LASTEXITCODE -ne 0) { throw "Could not read the installer counter on '$Branch'." }
if ((($current -join "`n") -replace '^﻿', '').Trim() -eq $bumped.Trim()) {
	Write-Host "The installer counter on '$Branch' is already $counterVersion; nothing to commit."
	exit 0
}

$head = gh api "repos/$ReleaseRepository/branches/$Branch" --jq '.commit.sha'
if ($LASTEXITCODE -ne 0) { throw "Could not read the head of '$Branch'." }

$payload = @{
	query     = 'mutation($input: CreateCommitOnBranchInput!) { createCommitOnBranch(input: $input) { commit { oid } } }'
	variables = @{ input = @{
			branch          = @{ repositoryNameWithOwner = $ReleaseRepository; branchName = $Branch }
			message         = @{ headline = "chore: bump installer version counter to $counterVersion (release $Release) [skip ci]" }
			expectedHeadOid = "$head".Trim()
			fileChanges     = @{ additions = @(@{
						path     = $CounterRepoPath
						contents = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($bumped))
					}) }
		} }
} | ConvertTo-Json -Depth 10

# Through a file, not a pipe, so no BOM sneaks in (the GraphQL endpoint rejects one).
$tempDirectory = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
$payloadPath = Join-Path $tempDirectory 'installer-counter-commit.json'
[IO.File]::WriteAllText($payloadPath, $payload, (New-Object Text.UTF8Encoding $false))

$oid = gh api graphql --input $payloadPath --jq '.data.createCommitOnBranch.commit.oid'
if ($LASTEXITCODE -ne 0 -or -not $oid) { throw "Committing the installer counter to '$Branch' failed." }

Write-Host "Committed installer counter $counterVersion to '$Branch' ($oid)."
Add-StepSummary @("Installer counter $counterVersion committed to ``$Branch`` ($oid).", '')
