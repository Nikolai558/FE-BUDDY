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
Remove-Item "$pubdir\WindowsBase.dll"
Remove-Item "$pubdir\DirectWriteForwarder.dll"
Remove-Item "$pubdir\WindowsFormsIntegration.dll"
Remove-Item "$pubdir\System.Xaml.dll"
Remove-Item "$pubdir\System.Windows.dll"
Remove-Item "$pubdir\System.Windows.Controls.Ribbon.dll"
Remove-Item "$pubdir\System.Windows.Extensions.dll"
Remove-Item "$pubdir\System.Windows.Presentation.dll"
Remove-Item "$pubdir\System.Windows.Input.Manipulations.dll"
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

Write-Output ""
Write-Output "Build Complete"