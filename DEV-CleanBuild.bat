@echo off

setlocal enabledelayedexpansion

rem Every bin and obj folder under .\FeBuddy, however deep, so a new project is covered too.
rem .\FeBuddy\releases holds the built MSI, so nothing under it is touched.
for /d /r ".\FeBuddy" %%d in (bin obj) do (
    if exist "%%d\" (
        set "folder=%%d"
        if /i "!folder:\FeBuddy\releases\=!"=="!folder!" (
            echo Deleting %%d
            rmdir /s /q "%%d"
        )
    )
)

rem build.ps1's publish output and coverage.ps1's results.
for %%d in (
    ".\FeBuddy\publish"
    ".\FeBuddy\TestResults"
) do (
    if exist "%%~d\" (
        echo Deleting %%~d
        rmdir /s /q "%%~d"
    ) else (
        echo %%~d does not exist.
    )
)

pause
