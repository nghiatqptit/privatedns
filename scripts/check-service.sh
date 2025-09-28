#!/bin/bash

# PrivateDNS Worker Service Status Checker
# This script checks the current service status

echo "PrivateDNS Worker Service Status"
echo "================================"

SERVICE_NAME="PrivateDNS"

# Check if running on Windows (Git Bash, WSL, etc.)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    echo "Platform: Windows"
    echo ""
    
    # Check if service exists and get its status
    if sc query "$SERVICE_NAME" > /dev/null 2>&1; then
        echo "Service Status:"
        sc query "$SERVICE_NAME"
        echo ""
        
        # Check if service is running
        if sc query "$SERVICE_NAME" | grep -q "RUNNING"; then
            echo "? PrivateDNS service is RUNNING"
        elif sc query "$SERVICE_NAME" | grep -q "STOPPED"; then
            echo "? PrivateDNS service is STOPPED"
        else
            echo "? PrivateDNS service status is UNKNOWN"
        fi
    else
        echo "? PrivateDNS service is NOT INSTALLED"
    fi
    
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Platform: Linux"
    echo ""
    
    # Check if service exists
    if systemctl list-unit-files | grep -q "privateDNS.service"; then
        echo "Service Status:"
        systemctl status privateDNS.service --no-pager -l
        echo ""
        
        # Check if service is active
        if systemctl is-active privateDNS.service > /dev/null 2>&1; then
            echo "? PrivateDNS service is RUNNING"
        else
            echo "? PrivateDNS service is STOPPED"
        fi
        
        # Check if service is enabled
        if systemctl is-enabled privateDNS.service > /dev/null 2>&1; then
            echo "? PrivateDNS service is ENABLED (auto-start)"
        else
            echo "? PrivateDNS service is DISABLED"
        fi
    else
        echo "? PrivateDNS service is NOT INSTALLED"
    fi
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Platform: macOS"
    echo ""
    
    # Check if LaunchDaemon exists
    if [[ -f "/Library/LaunchDaemons/com.privateDNS.service.plist" ]]; then
        echo "LaunchDaemon Status:"
        launchctl list | grep "com.privateDNS.service" || echo "Service not loaded"
        echo ""
        
        # Check if service is loaded
        if launchctl list | grep -q "com.privateDNS.service"; then
            echo "? PrivateDNS service is LOADED"
        else
            echo "? PrivateDNS service is NOT LOADED"
        fi
    else
        echo "? PrivateDNS service is NOT INSTALLED"
    fi
    
else
    echo "Platform: $OSTYPE (unsupported)"
    echo "This script supports Windows, Linux, and macOS"
fi

echo ""
echo "Service Management Scripts:"
echo "Install service:   ./install-service.sh (or .bat on Windows)"
echo "Uninstall service: ./uninstall-service.sh (or .bat on Windows)"