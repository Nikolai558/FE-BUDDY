# Stop the script if an error occurs
$ErrorActionPreference = "Stop"
$pubdir = "$PSScriptRoot\publish"
$releasedir = "$PSScriptRoot\releases"

# Ensure a clean state by removing build/package folders
$Folders = @($pubdir, $releasedir, "$PSScriptRoot\FeBuddyWinFormUI\obj", "$PSScriptRoot\FeBuddyWinFormUI\bin")
foreach ($Folder in $Folders) {
    if (Test-Path $Folder) {
        Remove-Item -path "$Folder" -Recurse -Force
    }
}

# Publish projects and remove unnecessary WPF files
dotnet publish -v minimal -c Release -r win-x64 --self-contained "$PSScriptRoot\FeBuddyWinFormUI\FeBuddyWinFormUI.csproj" -o "$pubdir"

if ($LASTEXITCODE -ne 0) {
    throw "FE-BUDDY publish failed with exit code $LASTEXITCODE."
}

# Remove unnecessary WPF assemblies if they were included in the publish output
$WpfAssemblies = @(
    "WindowsBase.dll",
    "DirectWriteForwarder.dll",
    "WindowsFormsIntegration.dll",
    "System.Xaml.dll",
    "System.Windows.dll",
    "System.Windows.Controls.Ribbon.dll",
    "System.Windows.Extensions.dll",
    "System.Windows.Presentation.dll",
    "System.Windows.Input.Manipulations.dll"
)

foreach ($Assembly in $WpfAssemblies) {
    $AssemblyPath = Join-Path $pubdir $Assembly

    if (Test-Path $AssemblyPath) {
        Remove-Item -Path $AssemblyPath -Force
    }
}
Get-ChildItem -Path "$pubdir" -Filter "*cor3*" | Remove-Item -Force -Recurse
Get-ChildItem -Path "$pubdir" -Filter "*Presentation*" | Remove-Item -Force -Recurse
Get-ChildItem -Path "$pubdir" -Filter "*UIAutomation*" | Remove-Item -Force -Recurse

# Get current product version from main dll FileVersion
$verObj = Get-ChildItem -Path "$pubdir\FE-BUDDY.dll" -Recurse | Select-Object -ExpandProperty VersionInfo
$ver = $verObj.ProductVersion
Write-Output "Building version $ver"

# Squirrel release
# Squirrel is being retired (see conversation/plan) and its CLI rejects any version with a
# -alpha/-beta/-rc tag, so this step is expected to fail for prerelease builds - that's
# tolerated, not fatal, and the MSI still gets built below regardless. Explicitly reset
# $LASTEXITCODE afterward so this native tool's exit code can't leak into later checks
# ($LASTEXITCODE is only ever set by native executables - it is NOT reset by invoking a
# .ps1 script that doesn't itself run one, so a stale value here would otherwise silently
# poison the next unrelated $LASTEXITCODE check).
Set-Alias Squirrel ($env:USERPROFILE + "\.nuget\packages\clowd.squirrel\2.9.42\tools\Squirrel.exe")
New-Item -Path "$PSScriptRoot" -Name "releases" -ItemType "directory"
Squirrel github-down --repoUrl "https://github.com/Nikolai558/FE-BUDDY" -r "$releasedir"
Squirrel pack -u "FE-BUDDY" -v "$ver" -p "$pubdir" -r "$releasedir"
$LASTEXITCODE = 0

# MSI release
Write-Output ""
Write-Output "Building FE-BUDDY MSI..."

# MSI's own ProductVersion can't hold a real semver string (e.g. "2.8.4-alpha.1") - see
# MSI-VERSION-NUMBERING.md. Get-InstallerVersion.ps1 derives the disposable internal
# counter version; the real version ($ver) is passed through separately as ProductSemVer.
$installerVersionCounterFile = "$PSScriptRoot\FE-BUDDY.Installer\installer-version-counter.json"
$installerVersion = & "$PSScriptRoot\FE-BUDDY.Installer\Get-InstallerVersion.ps1" `
    -RealVersion "$ver" `
    -CounterFilePath $installerVersionCounterFile

# No $LASTEXITCODE check here: Get-InstallerVersion.ps1 is a plain PowerShell script (not a
# native executable), so it never sets $LASTEXITCODE on success - and with
# $ErrorActionPreference = "Stop" (top of this script), a `throw` inside it already
# terminates this script on its own; there's nothing to check.

Write-Output "Real product version: $ver  ->  MSI internal version: $installerVersion"

dotnet build "$PSScriptRoot\FE-BUDDY.Installer\FE-BUDDY.Installer.wixproj" `
    -c Release `
    -p:InstallerVersion="$installerVersion" `
    -p:ProductSemVer="$ver"

if ($LASTEXITCODE -ne 0) {
    throw "FE-BUDDY MSI build failed with exit code $LASTEXITCODE."
}

$msiSource = "$PSScriptRoot\FE-BUDDY.Installer\bin\Release\en-US\FE-BUDDY.Installer.msi"
$msiDestination = "$releasedir\FE-BUDDY-$ver.msi"

Copy-Item -Path $msiSource -Destination $msiDestination -Force

Write-Output "MSI created: $msiDestination"

Write-Output ""
Write-Output "Build Complete"