@echo off
chcp 65001 >nul 2>&1

title Screen Time Controller - Protection Setup

set "SERVICE_NAME=ScreenTimeController_ProtectionService"
set "TASK_NAME=ScreenTimeController_Restart"
set "TASK_NAME_BOOT=ScreenTimeController_Restart_Boot"
set "APPDATA_DIR=%APPDATA%\ScreenTimeController"
set "COMMONDATA_DIR=%PROGRAMDATA%\ScreenTimeController"

:MENU
cls
echo ============================================
echo   Screen Time Controller - Protection Setup
echo ============================================
echo.
echo   1. Install Protection
echo   2. Uninstall Protection
echo   3. Start Service
echo   4. Check Status
echo   5. Exit
echo.
echo ============================================
set /p choice="Please select an option (1-5): "

if "%choice%"=="1" goto INSTALL
if "%choice%"=="2" goto UNINSTALL
if "%choice%"=="3" goto START_SERVICE
if "%choice%"=="4" goto CHECK_STATUS
if "%choice%"=="5" goto END

echo Invalid option. Please try again.
timeout /t 2 >nul
goto MENU

:INSTALL
cls
echo ============================================
echo   Installing Protection...
echo ============================================
echo.

echo [1/5] Stopping existing processes...
taskkill /f /im WatchdogMonitor.exe 2>nul
taskkill /f /im ProtectionService.exe 2>nul
timeout /t 2 >nul

echo [2/5] Creating data directories...
if not exist "%APPDATA_DIR%" mkdir "%APPDATA_DIR%"
if not exist "%COMMONDATA_DIR%" mkdir "%COMMONDATA_DIR%"

echo [3/5] Creating Windows service...
sc stop "%SERVICE_NAME%" 2>nul
sc delete "%SERVICE_NAME%" 2>nul
timeout /t 2 >nul
sc create "%SERVICE_NAME%" binPath= "%~dp0ProtectionService.exe" start= auto DisplayName= "Screen Time Controller Protection Service"

echo [4/5] Creating scheduled tasks...
schtasks /delete /tn "%TASK_NAME%" /f 2>nul
schtasks /delete /tn "%TASK_NAME_BOOT%" /f 2>nul
schtasks /create /tn "%TASK_NAME%" /tr "\"%~dp0WatchdogMonitor.exe\"" /sc onlogon /rl highest /f
schtasks /create /tn "%TASK_NAME_BOOT%" /tr "\"%~dp0WatchdogMonitor.exe\"" /sc onstart /rl highest /f

echo [5/5] Starting service and task...
sc start "%SERVICE_NAME%" 2>nul
schtasks /run /tn "%TASK_NAME%" 2>nul

echo.
echo ============================================
echo   Installation completed!
echo ============================================
echo.
pause
goto MENU

:UNINSTALL
cls
echo ============================================
echo   Uninstalling Protection...
echo ============================================
echo.

echo [1/8] Stopping ALL scheduled tasks...
schtasks /end /tn "ScreenTimeController_Restart" /f 2>nul
schtasks /end /tn "ScreenTimeController_Restart_Boot" /f 2>nul
schtasks /end /tn "\ScreenTimeController_Restart" /f 2>nul
schtasks /end /tn "\ScreenTimeController_Restart_Boot" /f 2>nul
timeout /t 2 >nul

echo [2/8] Finding and removing ALL scheduled tasks via PowerShell...
powershell -Command "Get-ScheduledTask | Where-Object { $_.TaskName -like '*ScreenTime*' -or $_.TaskName -like '*Watchdog*' } | ForEach-Object { Write-Host 'Found task:' $_.TaskPath$_.TaskName; Stop-ScheduledTask -TaskPath $_.TaskPath -TaskName $_.TaskName -ErrorAction SilentlyContinue; Unregister-ScheduledTask -TaskPath $_.TaskPath -TaskName $_.TaskName -Confirm:$false -ErrorAction SilentlyContinue }"
timeout /t 2 >nul

echo [3/8] Deleting scheduled tasks (all variations)...
schtasks /delete /tn "ScreenTimeController_Restart" /f 2>nul
schtasks /delete /tn "ScreenTimeController_Restart_Boot" /f 2>nul
schtasks /delete /tn "\ScreenTimeController_Restart" /f 2>nul
schtasks /delete /tn "\ScreenTimeController_Restart_Boot" /f 2>nul

echo [4/8] Stopping service...
sc stop "%SERVICE_NAME%" 2>nul
timeout /t 3 >nul

echo [5/8] Deleting service...
sc delete "%SERVICE_NAME%" 2>nul
timeout /t 2 >nul

echo [6/8] Killing ALL related processes (with elevation)...
taskkill /f /im WatchdogMonitor.exe 2>nul
taskkill /f /im ProtectionService.exe 2>nul
taskkill /f /im ScreenTimeController.exe 2>nul

echo [7/8] Force killing via PowerShell (if needed)...
powershell -Command "Get-Process -Name ScreenTimeController,ProtectionService,WatchdogMonitor -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue"
timeout /t 2 >nul

echo [8/8] Cleaning up ALL data directories...
echo Removing %ProgramData%\ScreenTimeController...
if exist "%COMMONDATA_DIR%" rd /s /q "%COMMONDATA_DIR%" 2>nul
echo Removing %AppData%\ScreenTimeController...
if exist "%APPDATA_DIR%" rd /s /q "%APPDATA_DIR%" 2>nul
echo Removing registry startup entry...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "ScreenTimeController" /f 2>nul
reg delete "HKLM\SOFTWARE\ScreenTimeController" /f 2>nul
echo Data cleanup completed.

echo.
echo ============================================
echo   Uninstallation completed!
echo ============================================
echo.
pause
goto MENU

:START_SERVICE
cls
echo ============================================
echo   Starting Protection...
echo ============================================
echo.

echo Starting service...
sc start "%SERVICE_NAME%" 2>nul

echo Starting scheduled task...
schtasks /run /tn "%TASK_NAME%" 2>nul

echo.
echo Protection started.
echo.
pause
goto MENU

:CHECK_STATUS
cls
echo ============================================
echo   Protection Status
echo ============================================
echo.

echo [Service Status]
sc query "%SERVICE_NAME%" 2>nul

echo.
echo [Scheduled Task Status]
schtasks /query /tn "%TASK_NAME%" 2>nul

echo.
schtasks /query /tn "%TASK_NAME_BOOT%" 2>nul

echo.
echo [Process Status]
tasklist /fi "imagename eq ScreenTimeController.exe" 2>nul | find "ScreenTimeController"
tasklist /fi "imagename eq ProtectionService.exe" 2>nul | find "ProtectionService"
tasklist /fi "imagename eq WatchdogMonitor.exe" 2>nul | find "WatchdogMonitor"

echo.
echo [Abnormal Exits Today]
if exist "%COMMONDATA_DIR%\abnormal_exits.txt" (
    type "%COMMONDATA_DIR%\abnormal_exits.txt"
) else (
    echo No abnormal exit records found.
)

echo.
echo ============================================
pause
goto MENU

:END
cls
echo.
echo Exiting...
timeout /t 1 >nul
exit /b 0
