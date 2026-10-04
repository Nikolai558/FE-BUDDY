<#
.SYNOPSIS
	Builds FE-Buddy's MSI installer: publishes the app, then packages it with WiX.

.DESCRIPTION
	1. Publishes FeBuddy.Wpf (Release, win-x64, self-contained) into publish\.
	2. Reads the real version from the built FE-BUDDY.dll's Product version - the csproj
	   <Version>, e.g. 3.0.0-alpha.1 - and checks it is SemVer.
	3. Gets the MSI's internal counter version from FeBuddy.Installer\Get-InstallerVersion.ps1,
	   which advances FeBuddy.Installer\installer-version-counter.json (the release workflow
	   commits that change to v3-development; nobody edits it by hand).
	4. Builds FeBuddy.Installer (and its custom action) with both numbers and copies the MSI to
	   releases\FE-BUDDY-Setup.msi. The name never changes, so
	   .../releases/latest/download/FE-BUDDY-Setup.msi always finds the latest stable release's
	   installer. The version is the release's tag (and the MSI's ProductSemVer property).

	See docs/Developers/VERSIONING.md. publish\ and
	releases\ are gitignored and recreated on every run.

.EXAMPLE
	.\build.ps1
#>

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$pubdir = Join-Path $root 'publish'
$releasedir = Join-Path $root 'releases'

foreach ($dir in @($pubdir, $releasedir)) {
	if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
}

# ------------------------------------------------------------------ publish

dotnet publish (Join-Path $root 'FeBuddy.Wpf\FeBuddy.Wpf.csproj') `
	-c Release `
	-r win-x64 `
	--self-contained `
	-o $pubdir `
	-v minimal
if ($LASTEXITCODE -ne 0) { throw "FE-Buddy publish failed with exit code $LASTEXITCODE." }

# ------------------------------------------------------------------ version

# The real version is the built app's Product version (the csproj <Version>). The app reads the
# same value at runtime, and the MSI records it as ProductSemVer for the next install's
# version-policy check - one source for all three.
$dll = Join-Path $pubdir 'FE-BUDDY.dll'
if (-not (Test-Path $dll)) { throw "Publish did not produce $dll." }

$ver = (Get-Item $dll).VersionInfo.ProductVersion
$ver = if ($ver) { $ver.Trim() } else { '' }
if ($ver -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$') {
	throw ("The built Product version '$ver' is not a SemVer MAJOR.MINOR.PATCH[-PRERELEASE] version. " +
		'Check <Version> in FeBuddy.Wpf\FeBuddy.Wpf.csproj (and that no "+metadata" is appended).')
}

Write-Output ''
Write-Output "Building FE-BUDDY $ver"

# The MSI's own ProductVersion cannot hold a pre-release tag, so it gets a disposable counter
# instead; the real version travels separately as ProductSemVer.
$installerVersion = & (Join-Path $root 'FeBuddy.Installer\Get-InstallerVersion.ps1') `
	-RealVersion $ver `
	-CounterFilePath (Join-Path $root 'FeBuddy.Installer\installer-version-counter.json')

Write-Output "Real product version: $ver  ->  MSI internal version: $installerVersion"

# ------------------------------------------------------------------ MSI

dotnet build (Join-Path $root 'FeBuddy.Installer\FeBuddy.Installer.wixproj') `
	-c Release `
	-v minimal `
	-p:InstallerVersion="$installerVersion" `
	-p:ProductSemVer="$ver"
if ($LASTEXITCODE -ne 0) { throw "FE-Buddy MSI build failed with exit code $LASTEXITCODE." }

$msiSource = Get-ChildItem (Join-Path $root 'FeBuddy.Installer\bin\Release') -Recurse -Filter 'FeBuddy.Installer.msi' |
	Sort-Object LastWriteTime -Descending |
	Select-Object -First 1
if (-not $msiSource) { throw 'The MSI build did not produce FeBuddy.Installer.msi.' }

New-Item -ItemType Directory -Path $releasedir -Force | Out-Null
$msiDestination = Join-Path $releasedir 'FE-BUDDY-Setup.msi'
Copy-Item -Path $msiSource.FullName -Destination $msiDestination -Force

Write-Output ''
Write-Output "MSI created: $msiDestination"
