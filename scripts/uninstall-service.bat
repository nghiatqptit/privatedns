@echo off
REM PrivateDNS Worker Service Uninstaller for Windows
REM This script removes PrivateDNS Windows Service
REM Requires administrator privileges

echo PrivateDNS Worker Service Uninstaller
echo =====================================

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click on this file and select "Run as administrator"
    pause
    exit /b 1
)

set SERVICE_NAME=PrivateDNS

echo Uninstalling PrivateDNS Windows Service...
echo.

REM Check if service exists
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% neq 0 (
    echo ? Service '%SERVICE_NAME%' not found
    echo This might be normal if no service was previously installed
    goto :end
)

REM Stop the service first
echo Stopping service...
sc stop "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% equ 0 (
    echo ? Service stopped
) else (
    echo ? Service was not running or failed to stop
)

REM Wait a moment for the service to stop
timeout /t 3 /nobreak >nul

REM Delete the service
echo Removing service...
sc delete "%SERVICE_NAME%"

if %errorLevel% equ 0 (
    echo ? Windows Service removed successfully!
) else (
    echo ? Failed to remove service
    echo The service might still be stopping. Try again in a few moments.
    pause
    exit /b 1
)

:end
echo.
echo Uninstall complete!
echo The PrivateDNS service has been removed from the system.
pause