@echo off
REM PrivateDNS GitHub Upload Setup Script for Windows
REM This script initializes git and prepares the project for GitHub upload

echo PrivateDNS GitHub Upload Setup
echo ==============================
echo.

REM Check if git is installed
where git >nul 2>nul
if %errorLevel% neq 0 (
    echo ? Git is not installed. Please install Git first.
    echo Download from: https://git-scm.com/
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('git --version') do set GIT_VERSION=%%i
echo ? Git found: %GIT_VERSION%
echo.

REM Get current directory
set PROJECT_DIR=%CD%
echo ?? Project directory: %PROJECT_DIR%
echo.

REM Check if already a git repository
if exist ".git" (
    echo ??  Git repository already exists
    echo Current remotes:
    git remote -v
    echo.
) else (
    echo ?? Initializing git repository...
    git init
    echo ? Git repository initialized
    echo.
)

REM Check git configuration
git config user.name >nul 2>nul
if %errorLevel% neq 0 (
    echo ??  Git user.name not configured
    set /p GITHUB_USERNAME=Enter your GitHub username: 
    git config user.name "%GITHUB_USERNAME%"
    echo ? Git user.name set to: %GITHUB_USERNAME%
)

git config user.email >nul 2>nul
if %errorLevel% neq 0 (
    echo ??  Git user.email not configured
    set /p GITHUB_EMAIL=Enter your GitHub email: 
    git config user.email "%GITHUB_EMAIL%"
    echo ? Git user.email set to: %GITHUB_EMAIL%
)

echo.
echo ?? Git configuration:
for /f "tokens=*" %%i in ('git config user.name') do echo Name: %%i
for /f "tokens=*" %%i in ('git config user.email') do echo Email: %%i
echo.

REM Add files to git
echo ?? Adding files to git...
git add .

REM Check git status
echo ?? Git status:
git status --short
echo.

REM Create initial commit
git diff --cached --quiet >nul 2>nul
if %errorLevel% equ 0 (
    echo ??  No changes to commit
) else (
    echo ?? Creating initial commit...
    git commit -m "Initial commit: PrivateDNS proxy service" -m "" -m "Features:" -m "- DNS proxy with domain whitelisting" -m "- Cross-platform system service support" -m "- Non-privileged operation (port 5353)" -m "- Optional port forwarding to standard DNS port (53)" -m "- Comprehensive management scripts" -m "- Support for Windows, Linux, and macOS"
    
    echo ? Initial commit created
)

echo.
echo ?? Next Steps:
echo ==============
echo 1. Create a new repository on GitHub:
echo    - Go to https://github.com/new
echo    - Repository name: PrivateDNS
echo    - Description: DNS proxy service with domain whitelisting and cross-platform system service support
echo    - Choose Public or Private
echo    - DO NOT initialize with README, .gitignore, or license (already exist)
echo.
echo 2. Connect to GitHub (replace YOUR_USERNAME):
echo    git remote add origin https://github.com/YOUR_USERNAME/PrivateDNS.git
echo    git branch -M main
echo    git push -u origin main
echo.
echo 3. Or if using SSH:
echo    git remote add origin git@github.com:YOUR_USERNAME/PrivateDNS.git
echo    git branch -M main
echo    git push -u origin main
echo.
echo ?? Repository will include:
echo - Complete PrivateDNS source code
echo - Cross-platform management scripts
echo - Comprehensive documentation
echo - MIT License
echo - .NET 8 Worker Service implementation
pause