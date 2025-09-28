@echo off
REM PrivateDNS Complete Setup Script for Windows
REM This script provides a complete setup wizard for PrivateDNS

echo ================================================
echo    PrivateDNS Complete Setup Wizard
echo ================================================
echo.
echo This wizard will help you set up PrivateDNS with:
echo 1. System service installation
echo 2. Optional port forwarding (standard DNS port 53)
echo 3. Configuration verification
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click on this file and select "Run as administrator"
    pause
    exit /b 1
)

echo Detected platform: Windows
echo Running with Administrator privileges ?
echo.

REM Get directories
set SCRIPT_DIR=%~dp0
for %%i in ("%SCRIPT_DIR%..") do set PROJECT_DIR=%%~fi

REM Step 1: Build project
echo Step 1: Building PrivateDNS project...
cd /d "%PROJECT_DIR%"
dotnet build -c Release

if %errorLevel% equ 0 (
    echo ? Project built successfully
    set BUILD_SUCCESS=true
) else (
    echo ? Failed to build project
    set BUILD_SUCCESS=false
    pause
    exit /b 1
)
echo.

REM Step 2: Install service
echo Step 2: Installing PrivateDNS as Windows service...
call "%SCRIPT_DIR%install-service.bat"

if %errorLevel% equ 0 (
    echo ? Service installed successfully
    set SERVICE_INSTALLED=true
) else (
    echo ? Failed to install service
    set SERVICE_INSTALLED=false
)
echo.

REM Step 3: Ask about port forwarding
echo Step 3: Port forwarding setup
echo Port forwarding allows DNS clients to use standard port 53
echo instead of the non-privileged port 5353.
echo.
set /p PORT_FORWARD=Do you want to set up port forwarding (53 -^> 5353)? (y/N): 

if /i "%PORT_FORWARD%"=="y" (
    echo Installing port forwarding...
    call "%SCRIPT_DIR%install-port-forwarding.bat"
    
    if %errorLevel% equ 0 (
        echo ? Port forwarding installed successfully
        set PORT_FORWARDING=true
    ) else (
        echo ? Failed to install port forwarding
        set PORT_FORWARDING=false
    )
) else (
    echo Skipping port forwarding setup
    set PORT_FORWARDING=false
)
echo.

REM Step 4: Configuration check
echo Step 4: Configuration verification...
set CONFIG_FILE=%PROJECT_DIR%\appsettings.json
set DOMAINS_FILE=%PROJECT_DIR%\allowed-domains.json

if exist "%CONFIG_FILE%" (
    echo ? Configuration file found: %CONFIG_FILE%
) else (
    echo ? Configuration file not found: %CONFIG_FILE%
)

if exist "%DOMAINS_FILE%" (
    echo ? Allowed domains file found: %DOMAINS_FILE%
    echo Current allowed domains:
    type "%DOMAINS_FILE%" | findstr "google\|microsoft\|github" | head -5
) else (
    echo ? Allowed domains file not found: %DOMAINS_FILE%
)
echo.

REM Step 5: Start service
if "%SERVICE_INSTALLED%"=="true" (
    set /p START_SERVICE=Do you want to start the PrivateDNS service now? (Y/n): 
    
    if not "%START_SERVICE%"=="n" if not "%START_SERVICE%"=="N" (
        echo Starting PrivateDNS service...
        sc start PrivateDNS
        
        REM Wait a moment and check status
        timeout /t 3 /nobreak >nul
        echo.
        echo Checking service status...
        call "%SCRIPT_DIR%check-service.bat"
    )
)

REM Summary
echo.
echo ================================================
echo    Setup Complete!
echo ================================================
echo.
echo Summary:
echo --------
if "%SERVICE_INSTALLED%"=="true" (
    echo ? PrivateDNS service installed
) else (
    echo ? Service installation failed
)

if "%PORT_FORWARDING%"=="true" (
    echo ? Port forwarding configured ^(53 -^> 5353^)
    echo   DNS clients can use: 127.0.0.1:53
    set DNS_ADDRESS=127.0.0.1
) else (
    echo ? Port forwarding not configured
    echo   DNS clients must use: 127.0.0.1:5353
    set DNS_ADDRESS=127.0.0.1:5353
)

echo.
echo Next Steps:
echo -----------
echo 1. Configure your DNS clients to use %DNS_ADDRESS%
echo 2. Edit %DOMAINS_FILE% to customize allowed domains
echo 3. Test with: nslookup google.com %DNS_ADDRESS%
echo.
echo Management Scripts:
echo -------------------
echo Check service status:  scripts\check-service.bat
echo Check port forwarding: scripts\check-port-forwarding.bat
echo Uninstall service:     scripts\uninstall-service.bat
echo Remove port forwarding: scripts\uninstall-port-forwarding.bat
echo.
echo For detailed documentation, see README.md and scripts\README.md
pause