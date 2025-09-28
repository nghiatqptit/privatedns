@echo off
REM PrivateDNS Port Forwarding Status Checker for Windows
REM This script checks the current port forwarding status

echo PrivateDNS Port Forwarding Status
echo =================================
echo Platform: Windows
echo.

echo Current port forwarding rules:
netsh interface portproxy show all
echo.

REM Check if our specific rule exists
netsh interface portproxy show all | findstr "53" | findstr "127.0.0.1" | findstr "5353" >nul
if %errorLevel% equ 0 (
    echo ? PrivateDNS port forwarding is ACTIVE ^(53 -^> 5353^)
) else (
    echo ? PrivateDNS port forwarding is NOT configured
)

echo.
echo To install port forwarding: install-port-forwarding.bat
echo To remove port forwarding:  uninstall-port-forwarding.bat
pause