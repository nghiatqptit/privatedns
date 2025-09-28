@echo off
REM PrivateDNS Port Forwarding Uninstaller for Windows
REM This script removes port forwarding from port 53 to 5353
REM Requires administrator privileges

echo PrivateDNS Port Forwarding Uninstaller
echo ======================================

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click on this file and select "Run as administrator"
    pause
    exit /b 1
)

echo Removing port forwarding: 53 -^> 5353
echo.

REM Remove port forwarding rule
netsh interface portproxy delete v4tov4 listenport=53 listenaddress=127.0.0.1

if %errorLevel% equ 0 (
    echo ? Port forwarding removed successfully!
    echo.
    echo To view remaining port forwarding rules:
    echo netsh interface portproxy show all
) else (
    echo ? No port forwarding rule found or failed to remove
    echo This might be normal if no rule was previously installed
)

echo.
echo Uninstall complete!
echo You can now use PrivateDNS with direct port configuration ^(127.0.0.1:5353^)
pause