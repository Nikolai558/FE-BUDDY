@echo off

setlocal enabledelayedexpansion

for %%d in (
    ".\FeBuddy\FEBuddyLibrary\bin"
    ".\FeBuddy\FeBuddyWPF\bin"
    ".\FeBuddy\UnitTests\bin"
    ".\FeBuddy\WPF_Template\bin"
    ".\FeBuddy\WPF_Template.Core\bin"
    ".\FeBuddy\WPF_Template.Tests.xUnit\bin"
) do (
    if exist "%%d" (
        echo Deleting all files and folders in %%d
        for /D %%i in ("%%d\*") do rmdir "%%i" /s /q
        del /q "%%d\*.*"
    ) else (
        echo %%d does not exist.
    )
)

pause
