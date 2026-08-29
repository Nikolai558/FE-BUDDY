@echo off

cd /d "%userprofile%/Desktop"

ECHO DELETED:

FOR /D %%X IN (*FE-BUDDY_Output*) DO RMDIR /S /Q "%%~fX" && ECHO     -  "%%~fX"