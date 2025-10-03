@echo off
REM PrivateDNS Test Runner Script for Windows
REM This script runs all unit tests with coverage reporting

echo PrivateDNS Test Suite
echo ====================
echo.

REM Check if dotnet is available
where dotnet >nul 2>nul
if %errorLevel% neq 0 (
    echo ? .NET SDK not found. Please install .NET 8 SDK.
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo ? .NET SDK found: %DOTNET_VERSION%
echo.

REM Get directories
set SCRIPT_DIR=%~dp0
for %%i in ("%SCRIPT_DIR%..") do set PROJECT_DIR=%%~fi
set TEST_PROJECT=%PROJECT_DIR%\PrivateDNS.Tests

echo ?? Project directory: %PROJECT_DIR%
echo ?? Test project: %TEST_PROJECT%
echo.

REM Check if test project exists
if not exist "%TEST_PROJECT%\PrivateDNS.Tests.csproj" (
    echo ? Test project not found at %TEST_PROJECT%
    pause
    exit /b 1
)

echo ?? Restoring packages...
cd /d "%PROJECT_DIR%"
dotnet restore

if %errorLevel% neq 0 (
    echo ? Failed to restore packages
    pause
    exit /b 1
)

echo.
echo ???  Building solution...
dotnet build --configuration Release --no-restore

if %errorLevel% neq 0 (
    echo ? Build failed
    pause
    exit /b 1
)

echo.
echo ?? Running tests...

REM Run tests
cd /d "%TEST_PROJECT%"

echo Running basic tests...
dotnet test --configuration Release --no-build --verbosity normal --logger "console;verbosity=detailed"

if %errorLevel% equ 0 (
    echo ? All tests passed!
) else (
    echo ? Some tests failed!
    pause
    exit /b 1
)

echo.
echo ?? Running tests with coverage...

REM Run with coverage
dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory ./TestResults

if %errorLevel% equ 0 (
    echo ? Tests with coverage completed!
    
    REM Check if coverage files were generated
    if exist "TestResults\*\coverage.cobertura.xml" (
        echo ?? Coverage report generated in TestResults\
    )
) else (
    echo ?? Tests with coverage had issues, but tests may have passed
)

echo.
echo ?? Test Categories Run:
echo - ? Unit Tests ^(Models, Services^)
echo - ? Integration Tests
echo - ? Performance Tests
echo.

echo ?? Test Summary:
echo - DNS Message parsing and serialization
echo - Domain configuration and matching
echo - DNS forwarding logic
echo - Service lifecycle management
echo - Performance and memory usage
echo.

echo ?? Test run completed!

REM Optional: Generate coverage report if reportgenerator is available
where reportgenerator >nul 2>nul
if %errorLevel% equ 0 (
    echo.
    echo ?? Generating HTML coverage report...
    if exist "TestResults\*\coverage.cobertura.xml" (
        reportgenerator -reports:"TestResults\*\coverage.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:Html
        echo ? HTML coverage report available at: TestResults\CoverageReport\index.html
    )
)

pause