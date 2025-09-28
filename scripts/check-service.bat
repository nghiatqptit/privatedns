@echo off
REM PrivateDNS Worker Service Status Checker for Windows
REM This script checks the current service status

echo PrivateDNS Worker Service Status
echo ================================
echo Platform: Windows
echo.

set SERVICE_NAME=PrivateDNS

REM Check if service exists
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% neq 0 (
    echo ? PrivateDNS service is NOT INSTALLED
    goto :end
)

echo Service Status:
sc query "%SERVICE_NAME%"
echo.

REM Check if service is running
sc query "%SERVICE_NAME%" | findstr "RUNNING" >nul
if %errorLevel% equ 0 (
    echo ? PrivateDNS service is RUNNING
) else (
    sc query "%SERVICE_NAME%" | findstr "STOPPED" >nul
    if %errorLevel% equ 0 (
        echo ? PrivateDNS service is STOPPED
    ) else (
        echo ? PrivateDNS service status is UNKNOWN
    )
)

REM Check startup type
echo.
echo Service Configuration:
sc qc "%SERVICE_NAME%"

:end
echo.
echo Service Management Scripts:
echo Install service:   install-service.bat
echo Uninstall service: uninstall-service.bat
echo.
echo Service Management Commands:
echo Start service:   sc start %SERVICE_NAME%
echo Stop service:    sc stop %SERVICE_NAME%
echo Service status:  sc query %SERVICE_NAME%
pause