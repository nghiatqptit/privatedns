@echo off
REM PrivateDNS Port Forwarding Installer for Windows
REM This script sets up port forwarding from port 53 to 5353
REM Requires administrator privileges

echo PrivateDNS Port Forwarding Installer
echo ====================================

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click on this file and select "Run as administrator"
    pause
    exit /b 1
)

echo Setting up port forwarding: 53 -^> 5353
echo.

REM Add port forwarding rule
netsh interface portproxy add v4tov4 listenport=53 listenaddress=127.0.0.1 connectport=5353 connectaddress=127.0.0.1

if %errorLevel% equ 0 (
    echo ? Port forwarding installed successfully!
    echo.
    echo Port 53 ^(127.0.0.1^) -^> Port 5353 ^(127.0.0.1^)
    echo.
    echo You can now:
    echo 1. Start PrivateDNS service: dotnet run
    echo 2. Configure your DNS to use 127.0.0.1 ^(standard port 53^)
    echo 3. Test with: nslookup google.com 127.0.0.1
    echo.
    echo To view all port forwarding rules:
    echo netsh interface portproxy show all
) else (
    echo ? Failed to install port forwarding
    echo Make sure you're running as Administrator
    pause
    exit /b 1
)

echo.
echo Installation complete!
echo Run 'uninstall-port-forwarding.bat' to remove port forwarding.
pause