@echo off
PING 127.0.0.1 - n 5 > nul
tasklist /FI "IMAGENAME eq FE-BUDDY.exe" 2>NUL | find /I /N "FE-BUDDY.exe">NUL
if "%ERRORLEVEL%"=="0" taskkill /F /im FE-BUDDY.exe

TITLE FE-BUDDY UNINSTALL

SET /A NOT_FOUND_COUNT=0

CD /d "%temp%"
	if NOT exist FE-BUDDY (
		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1
		SET FE-BUDDY_TEMP_FOLDER=NOT_FOUND
	)
	
	if exist FE-BUDDY (
		SET FE-BUDDY_TEMP_FOLDER=FOUND
		RD /Q /S "FE-BUDDY"
	)

CD /d "%userprofile%\\AppData\\Local"
	if NOT exist FE-BUDDY (
		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1
		SET FE-BUDDY_APPDATA_FOLDER=NOT_FOUND
	)
	
	if exist FE-BUDDY (
		SET FE-BUDDY_APPDATA_FOLDER=FOUND
		RD /Q /S "FE-BUDDY"
	)

CD /d "%userprofile%\\Desktop"
	if NOT exist FE-BUDDY.lnk (
		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1
		SET FE-BUDDY_SHORTCUT=NOT_FOUND
	)

	if exist FE-BUDDY.lnk (
		SET FE-BUDDY_SHORTCUT=FOUND
		DEL /Q "FE-BUDDY.lnk"
	)

CD /d "%appdata%\\Microsoft\\Windows\\Start Menu\\Programs"
 if NOT exist "Kyle Sanders" (
     SET OLD_START_SHORTCUT=NOT_FOUND
)

	if exist "Kyle Sanders" (
		SET OLD_START_SHORTCUT=FOUND
		RD /Q /S "Kyle Sanders"
	)

	if NOT exist FE-BUDDY.lnk (
		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1
		SET NEW_START_SHORTCUT=NOT_FOUND
	)

	if exist FE-BUDDY.lnk (
		SET NEW_START_SHORTCUT=FOUND
		DEL /Q "FE-BUDDY.lnk"
	)

IF %NOT_FOUND_COUNT%==0 SET UNINSTALL_STATUS=COMPLETE
IF %NOT_FOUND_COUNT% GEQ 1 SET UNINSTALL_STATUS=PARTIAL
IF %NOT_FOUND_COUNT%==4 SET UNINSTALL_STATUS=FAIL

IF %UNINSTALL_STATUS%==COMPLETE GOTO UNINSTALLED
IF %UNINSTALL_STATUS%==PARTIAL GOTO UNINSTALLED
IF %UNINSTALL_STATUS%==FAIL GOTO FAILED

CLS

:UNINSTALLED

ECHO.
ECHO.
ECHO SUCCESSFULLY UNINSTALLED THE FOLLOWING:
ECHO.
IF %FE-BUDDY_TEMP_FOLDER%==FOUND ECHO        -temp\\FE-BUDDY
IF %FE-BUDDY_APPDATA_FOLDER%==FOUND ECHO        -AppData\\Local\\FE-BUDDY
IF %FE-BUDDY_SHORTCUT%==FOUND ECHO        -Desktop\\FE-BUDDY Shortcut
IF %OLD_START_SHORTCUT%==FOUND ECHO        -Start Menu\\Kyle Sanders
IF %NEW_START_SHORTCUT%==FOUND ECHO        -Start Menu\\FE-BUDDY Shortcut

:FAILED

IF NOT %NOT_FOUND_COUNT%==0 (
	ECHO.
	ECHO.
	ECHO.
	ECHO.
	IF %UNINSTALL_STATUS%==PARTIAL ECHO NOT ABLE TO COMPLETELY UNINSTALL BECAUSE THE FOLLOWING COULD NOT BE FOUND:
	IF %UNINSTALL_STATUS%==FAIL ECHO UNINSTALL FAILED COMPLETELY BECAUSE THE FOLLOWING COULD NOT BE FOUND:
	ECHO.
	IF %FE-BUDDY_TEMP_FOLDER%==NOT_FOUND ECHO        -temp\\FE-BUDDY
	IF %FE-BUDDY_APPDATA_FOLDER%==NOT_FOUND ECHO        -AppData\\Local\\FE-BUDDY
	IF %FE-BUDDY_SHORTCUT%==NOT_FOUND (
		ECHO        -Desktop\\FE-BUDDY Shortcut
		ECHO             --If the shortcut was renamed, delete the shortcut manually.
	)
 IF %NEW_START_SHORTCUT%==NOT_FOUND ECHO        -Start Menu\\FE-BUDDY Shortcut
)

ECHO.
ECHO.
ECHO.
ECHO.
ECHO.
ECHO ...Close this prompt when ready.

PAUSE>NUL;