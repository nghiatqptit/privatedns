#!/bin/bash

# PrivateDNS Complete Setup Script
# This script provides a complete setup wizard for PrivateDNS

echo "================================================"
echo "    PrivateDNS Complete Setup Wizard"
echo "================================================"
echo ""
echo "This wizard will help you set up PrivateDNS with:"
echo "1. System service installation"
echo "2. Optional port forwarding (standard DNS port 53)"
echo "3. Configuration verification"
echo ""

# Get the current directory (where the script is located)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Check platform and privileges
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    PLATFORM="Windows"
    if ! net session > /dev/null 2>&1; then
        echo "ERROR: This script must be run as Administrator on Windows!"
        echo "Right-click on Git Bash/Terminal and select 'Run as administrator'"
        exit 1
    fi
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORM="Linux"
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root on Linux!"
        echo "Run with: sudo $0"
        exit 1
    fi
elif [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="macOS"
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root on macOS!"
        echo "Run with: sudo $0"
        exit 1
    fi
else
    echo "Unsupported platform: $OSTYPE"
    exit 1
fi

echo "Detected platform: $PLATFORM"
echo "Running with appropriate privileges ?"
echo ""

# Step 1: Build project
echo "Step 1: Building PrivateDNS project..."
cd "$PROJECT_DIR"
if dotnet build -c Release; then
    echo "? Project built successfully"
else
    echo "? Failed to build project"
    exit 1
fi
echo ""

# Step 2: Install service
echo "Step 2: Installing PrivateDNS as system service..."
if "$SCRIPT_DIR/install-service.sh"; then
    echo "? Service installed successfully"
    SERVICE_INSTALLED=true
else
    echo "? Failed to install service"
    SERVICE_INSTALLED=false
fi
echo ""

# Step 3: Ask about port forwarding
echo "Step 3: Port forwarding setup"
echo "Port forwarding allows DNS clients to use standard port 53"
echo "instead of the non-privileged port 5353."
echo ""
read -p "Do you want to set up port forwarding (53 -> 5353)? (y/N): " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Installing port forwarding..."
    if "$SCRIPT_DIR/install-port-forwarding.sh"; then
        echo "? Port forwarding installed successfully"
        PORT_FORWARDING=true
    else
        echo "? Failed to install port forwarding"
        PORT_FORWARDING=false
    fi
else
    echo "Skipping port forwarding setup"
    PORT_FORWARDING=false
fi
echo ""

# Step 4: Configuration check
echo "Step 4: Configuration verification..."
CONFIG_FILE="$PROJECT_DIR/appsettings.json"
DOMAINS_FILE="$PROJECT_DIR/allowed-domains.json"

if [[ -f "$CONFIG_FILE" ]]; then
    echo "? Configuration file found: $CONFIG_FILE"
else
    echo "? Configuration file not found: $CONFIG_FILE"
fi

if [[ -f "$DOMAINS_FILE" ]]; then
    echo "? Allowed domains file found: $DOMAINS_FILE"
    echo "Current allowed domains:"
    if command -v jq > /dev/null; then
        jq -r '.[]' "$DOMAINS_FILE" 2>/dev/null | head -5 | sed 's/^/  - /'
    else
        grep -o '"[^"]*"' "$DOMAINS_FILE" | head -5 | sed 's/"//g' | sed 's/^/  - /'
    fi
else
    echo "? Allowed domains file not found: $DOMAINS_FILE"
fi
echo ""

# Step 5: Start service
if [[ "$SERVICE_INSTALLED" == true ]]; then
    read -p "Do you want to start the PrivateDNS service now? (Y/n): " -n 1 -r
    echo ""
    
    if [[ ! $REPLY =~ ^[Nn]$ ]]; then
        echo "Starting PrivateDNS service..."
        
        if [[ "$PLATFORM" == "Windows" ]]; then
            sc start PrivateDNS
        elif [[ "$PLATFORM" == "Linux" ]]; then
            systemctl start privateDNS.service
        elif [[ "$PLATFORM" == "macOS" ]]; then
            launchctl start com.privateDNS.service
        fi
        
        # Wait a moment and check status
        sleep 2
        echo ""
        echo "Checking service status..."
        "$SCRIPT_DIR/check-service.sh"
    fi
fi

# Summary
echo ""
echo "================================================"
echo "    Setup Complete!"
echo "================================================"
echo ""
echo "Summary:"
echo "--------"
if [[ "$SERVICE_INSTALLED" == true ]]; then
    echo "? PrivateDNS service installed"
else
    echo "? Service installation failed"
fi

if [[ "$PORT_FORWARDING" == true ]]; then
    echo "? Port forwarding configured (53 -> 5353)"
    echo "  DNS clients can use: 127.0.0.1:53"
else
    echo "? Port forwarding not configured"
    echo "  DNS clients must use: 127.0.0.1:5353"
fi

echo ""
echo "Next Steps:"
echo "-----------"
echo "1. Configure your DNS clients to use 127.0.0.1$([ "$PORT_FORWARDING" == true ] && echo ":53" || echo ":5353")"
echo "2. Edit $DOMAINS_FILE to customize allowed domains"
echo "3. Test with: nslookup google.com 127.0.0.1$([ "$PORT_FORWARDING" == true ] && echo "" || echo ":5353")"
echo ""
echo "Management Scripts:"
echo "-------------------"
echo "Check service status:  ./scripts/check-service.sh"
echo "Check port forwarding: ./scripts/check-port-forwarding.sh"
echo "Uninstall service:     ./scripts/uninstall-service.sh"
echo "Remove port forwarding: ./scripts/uninstall-port-forwarding.sh"
echo ""
echo "For detailed documentation, see README.md and scripts/README.md"