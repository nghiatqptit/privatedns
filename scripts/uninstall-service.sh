#!/bin/bash

# PrivateDNS Worker Service Uninstaller
# This script removes PrivateDNS system service
# Requires administrator/root privileges

echo "PrivateDNS Worker Service Uninstaller"
echo "====================================="

SERVICE_NAME="PrivateDNS"

# Check if running on Windows (Git Bash, WSL, etc.)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    echo "Detected Windows environment"
    
    # Check if running as administrator
    if ! net session > /dev/null 2>&1; then
        echo "ERROR: This script must be run as Administrator!"
        echo "Right-click on Git Bash/Terminal and select 'Run as administrator'"
        exit 1
    fi
    
    echo "Uninstalling PrivateDNS Windows Service..."
    
    # Stop the service first
    echo "Stopping service..."
    sc stop "$SERVICE_NAME" 2>/dev/null || echo "Service was not running"
    
    # Wait a moment for the service to stop
    sleep 2
    
    # Delete the service
    if sc delete "$SERVICE_NAME"; then
        echo "? Windows Service removed successfully!"
    else
        echo "? Service not found or failed to remove"
        echo "This might be normal if no service was previously installed"
    fi
    
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Detected Linux environment"
    
    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root!"
        echo "Run with: sudo $0"
        exit 1
    fi
    
    echo "Uninstalling PrivateDNS systemd service..."
    
    # Stop and disable the service
    systemctl stop privateDNS.service 2>/dev/null || echo "Service was not running"
    systemctl disable privateDNS.service 2>/dev/null || echo "Service was not enabled"
    
    # Remove the service file
    if rm -f /etc/systemd/system/privateDNS.service; then
        echo "? systemd service file removed"
    fi
    
    # Reload systemd
    systemctl daemon-reload
    systemctl reset-failed
    
    echo "? systemd service removed successfully!"
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Detected macOS environment"
    
    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root!"
        echo "Run with: sudo $0"
        exit 1
    fi
    
    echo "Uninstalling PrivateDNS LaunchDaemon..."
    
    # Unload the service
    launchctl unload /Library/LaunchDaemons/com.privateDNS.service.plist 2>/dev/null || echo "Service was not loaded"
    
    # Remove the plist file
    if rm -f /Library/LaunchDaemons/com.privateDNS.service.plist; then
        echo "? LaunchDaemon plist removed"
    fi
    
    # Remove log files
    rm -f /var/log/privateDNS.out /var/log/privateDNS.err
    
    echo "? LaunchDaemon removed successfully!"
    
else
    echo "Unsupported operating system: $OSTYPE"
    echo "This script supports Windows, Linux, and macOS"
    exit 1
fi

echo ""
echo "Uninstall complete!"
echo "The PrivateDNS service has been removed from the system."