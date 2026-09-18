@echo off

setlocal enabledelayedexpansion

for %%d in (
    ".\FeBuddy\FeBuddy.Core\bin"
    ".\FeBuddy\FeBuddy.Wpf\bin"
    ".\FeBuddy\FeBuddy.Harness\bin"
    ".\FeBuddy\FeBuddy.UnitTests\bin"
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
