@echo off
REM PrivateDNS Worker Service Installer for Windows
REM This script installs PrivateDNS as a Windows Service
REM Requires administrator privileges

echo PrivateDNS Worker Service Installer
echo ===================================

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click on this file and select "Run as administrator"
    pause
    exit /b 1
)

echo Installing PrivateDNS as Windows Service...
echo.

REM Get the current directory (parent of scripts folder)
set SCRIPT_DIR=%~dp0
for %%i in ("%SCRIPT_DIR%..") do set PROJECT_DIR=%%~fi

echo Project Directory: %PROJECT_DIR%
echo.

REM Build the project first
echo Building project...
cd /d "%PROJECT_DIR%"
dotnet build -c Release

if %errorLevel% neq 0 (
    echo ? Failed to build project
    pause
    exit /b 1
)

REM Define service details
set SERVICE_NAME=PrivateDNS
set EXECUTABLE_PATH=%PROJECT_DIR%\bin\Release\net8.0\PrivateDNS.exe
set SERVICE_DESCRIPTION=PrivateDNS Proxy Service - DNS filtering with domain whitelisting

echo Creating Windows Service: %SERVICE_NAME%
echo Executable: %EXECUTABLE_PATH%
echo.

REM Create the service
sc create "%SERVICE_NAME%" binPath= "\"%EXECUTABLE_PATH%\"" DisplayName= "PrivateDNS Proxy Service" description= "%SERVICE_DESCRIPTION%" start= auto

if %errorLevel% equ 0 (
    echo ? Windows Service created successfully!
    echo.
    
    REM Set service to restart on failure
    sc failure "%SERVICE_NAME%" reset= 30 actions= restart/5000/restart/5000/restart/5000
    
    echo Service Management Commands:
    echo Start service:   sc start %SERVICE_NAME%
    echo Stop service:    sc stop %SERVICE_NAME%
    echo Service status:  sc query %SERVICE_NAME%
    echo Delete service:  sc delete %SERVICE_NAME%
    echo.
    echo Or use Services.msc GUI to manage the service
    echo.
    
    REM Ask if user wants to start the service now
    set /p START_NOW=Start the service now? (Y/N): 
    if /i "%START_NOW%"=="Y" (
        echo Starting service...
        sc start "%SERVICE_NAME%"
        if %errorLevel% equ 0 (
            echo ? Service started successfully!
        ) else (
            echo ? Failed to start service. Check the service status.
        )
    )
) else (
    echo ? Failed to create Windows Service
    echo Make sure you're running as Administrator
    pause
    exit /b 1
)

echo.
echo Installation complete!
echo Run 'uninstall-service.bat' to remove the service.
echo.
echo Note: The service will run on port 5353 by default ^(non-privileged^).
echo Use port forwarding scripts if you need standard DNS port 53.
pause