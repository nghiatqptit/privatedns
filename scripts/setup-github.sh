#!/bin/bash

# PrivateDNS GitHub Upload Setup Script
# This script initializes git and prepares the project for GitHub upload

echo "PrivateDNS GitHub Upload Setup"
echo "=============================="
echo ""

# Check if git is installed
if ! command -v git &> /dev/null; then
    echo "? Git is not installed. Please install Git first."
    echo "Download from: https://git-scm.com/"
    exit 1
fi

echo "? Git found: $(git --version)"
echo ""

# Get current directory
PROJECT_DIR=$(pwd)
echo "?? Project directory: $PROJECT_DIR"
echo ""

# Check if already a git repository
if [ -d ".git" ]; then
    echo "??  Git repository already exists"
    echo "Current remotes:"
    git remote -v
    echo ""
else
    echo "?? Initializing git repository..."
    git init
    echo "? Git repository initialized"
    echo ""
fi

# Check git configuration
if ! git config user.name &> /dev/null; then
    echo "??  Git user.name not configured"
    read -p "Enter your GitHub username: " GITHUB_USERNAME
    git config user.name "$GITHUB_USERNAME"
    echo "? Git user.name set to: $GITHUB_USERNAME"
fi

if ! git config user.email &> /dev/null; then
    echo "??  Git user.email not configured"
    read -p "Enter your GitHub email: " GITHUB_EMAIL
    git config user.email "$GITHUB_EMAIL"
    echo "? Git user.email set to: $GITHUB_EMAIL"
fi

echo ""
echo "?? Git configuration:"
echo "Name: $(git config user.name)"
echo "Email: $(git config user.email)"
echo ""

# Add files to git
echo "?? Adding files to git..."
git add .

# Check git status
echo "?? Git status:"
git status --short
echo ""

# Create initial commit
if git diff --cached --quiet; then
    echo "??  No changes to commit"
else
    echo "?? Creating initial commit..."
    git commit -m "Initial commit: PrivateDNS proxy service

Features:
- DNS proxy with domain whitelisting
- Cross-platform system service support
- Non-privileged operation (port 5353)
- Optional port forwarding to standard DNS port (53)
- Comprehensive management scripts
- Support for Windows, Linux, and macOS"
    
    echo "? Initial commit created"
fi

echo ""
echo "?? Next Steps:"
echo "=============="
echo "1. Create a new repository on GitHub:"
echo "   - Go to https://github.com/new"
echo "   - Repository name: PrivateDNS"
echo "   - Description: DNS proxy service with domain whitelisting and cross-platform system service support"
echo "   - Choose Public or Private"
echo "   - DO NOT initialize with README, .gitignore, or license (already exist)"
echo ""
echo "2. Connect to GitHub (replace YOUR_USERNAME):"
echo "   git remote add origin https://github.com/YOUR_USERNAME/PrivateDNS.git"
echo "   git branch -M main"
echo "   git push -u origin main"
echo ""
echo "3. Or if using SSH:"
echo "   git remote add origin git@github.com:YOUR_USERNAME/PrivateDNS.git"
echo "   git branch -M main"
echo "   git push -u origin main"
echo ""
echo "?? Repository will include:"
echo "- Complete PrivateDNS source code"
echo "- Cross-platform management scripts"
echo "- Comprehensive documentation"
echo "- MIT License"
echo "- .NET 8 Worker Service implementation"