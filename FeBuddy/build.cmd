@echo off
rem Builds FE-Buddy's MSI installer - see build.ps1.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
pause
