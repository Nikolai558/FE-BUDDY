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
Set-Alias Squirrel ($env:USERPROFILE + "\.nuget\packages\clowd.squirrel\2.9.42\tools\Squirrel.exe")
New-Item -Path "$PSScriptRoot" -Name "releases" -ItemType "directory"
Squirrel github-down --repoUrl "https://github.com/Nikolai558/FE-BUDDY" -r "$releasedir"
Squirrel pack -u "FE-BUDDY" -v "$ver" -p "$pubdir" -r "$releasedir"

# MSI release
Write-Output ""
Write-Output "Building FE-BUDDY MSI..."

dotnet build "$PSScriptRoot\FE-BUDDY.Installer\FE-BUDDY.Installer.wixproj" `
    -c Release `
    -p:InstallerVersion="$ver"

if ($LASTEXITCODE -ne 0) {
    throw "FE-BUDDY MSI build failed with exit code $LASTEXITCODE."
}

$msiSource = "$PSScriptRoot\FE-BUDDY.Installer\bin\Release\en-US\FE-BUDDY.Installer.msi"
$msiDestination = "$releasedir\FE-BUDDY-$ver.msi"

Copy-Item -Path $msiSource -Destination $msiDestination -Force

Write-Output "MSI created: $msiDestination"

Write-Output ""
Write-Output "Build Complete"