@echo off
REM Windows Update Helper Script
REM Arguments: PID NEW_PATH APP_PATH

set PID=%1
set NEW_PATH=%~2
set APP_PATH=%~3

echo Aether Update Helper (Windows)
echo Waiting for process %PID% to exit...

:wait
tasklist /FI "PID eq %PID%" 2>NUL | find "%PID%" >NUL
if not errorlevel 1 (
    timeout /t 1 /nobreak >NUL
    goto wait
)

echo Process exited. Updating application...

REM Remove old files and copy new
xcopy /E /Y /I "%NEW_PATH%\*" "%APP_PATH%\"

REM Clean up temp folder
rmdir /S /Q "%NEW_PATH%\.."

echo Update complete. Relaunching...

REM Relaunch
start "" "%APP_PATH%\Aether.exe"
