#!/bin/bash

# PrivateDNS Worker Service Installer
# This script installs PrivateDNS as a system service
# Requires administrator/root privileges

echo "PrivateDNS Worker Service Installer"
echo "==================================="

# Get the current directory (where the script is located)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
SERVICE_NAME="PrivateDNS"
SERVICE_DESCRIPTION="PrivateDNS Proxy Service - DNS filtering with domain whitelisting"

# Check if running on Windows (Git Bash, WSL, etc.)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    echo "Detected Windows environment"
    
    # Check if running as administrator
    if ! net session > /dev/null 2>&1; then
        echo "ERROR: This script must be run as Administrator!"
        echo "Right-click on Git Bash/Terminal and select 'Run as administrator'"
        exit 1
    fi
    
    echo "Installing PrivateDNS as Windows Service..."
    
    # Build the project first
    echo "Building project..."
    cd "$PROJECT_DIR"
    if ! dotnet build -c Release; then
        echo "? Failed to build project"
        exit 1
    fi
    
    # Create the service using sc command
    EXECUTABLE_PATH="$PROJECT_DIR\\bin\\Release\\net8.0\\PrivateDNS.exe"
    
    # Convert to Windows path format
    EXECUTABLE_PATH=$(echo "$EXECUTABLE_PATH" | sed 's|/|\\|g')
    
    echo "Creating Windows Service: $SERVICE_NAME"
    if sc create "$SERVICE_NAME" binPath= "\"$EXECUTABLE_PATH\"" DisplayName= "PrivateDNS Proxy Service" description= "$SERVICE_DESCRIPTION" start= auto; then
        echo "? Windows Service created successfully!"
        
        # Set service to restart on failure
        sc failure "$SERVICE_NAME" reset= 30 actions= restart/5000/restart/5000/restart/5000
        
        echo ""
        echo "Service Management Commands:"
        echo "Start service:   sc start $SERVICE_NAME"
        echo "Stop service:    sc stop $SERVICE_NAME"
        echo "Service status:  sc query $SERVICE_NAME"
        echo "Delete service:  sc delete $SERVICE_NAME"
        echo ""
        echo "Or use Services.msc GUI to manage the service"
    else
        echo "? Failed to create Windows Service"
        exit 1
    fi
    
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Detected Linux environment"
    
    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root!"
        echo "Run with: sudo $0"
        exit 1
    fi
    
    echo "Installing PrivateDNS as systemd service..."
    
    # Build the project first
    echo "Building project..."
    cd "$PROJECT_DIR"
    if ! dotnet build -c Release; then
        echo "? Failed to build project"
        exit 1
    fi
    
    # Create systemd service file
    cat > /etc/systemd/system/privateDNS.service << EOF
[Unit]
Description=$SERVICE_DESCRIPTION
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet $PROJECT_DIR/bin/Release/net8.0/PrivateDNS.dll
WorkingDirectory=$PROJECT_DIR
Restart=on-failure
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=privateDNS
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF
    
    # Reload systemd and enable the service
    systemctl daemon-reload
    systemctl enable privateDNS.service
    
    echo "? systemd service installed successfully!"
    echo ""
    echo "Service Management Commands:"
    echo "Start service:   sudo systemctl start privateDNS"
    echo "Stop service:    sudo systemctl stop privateDNS"
    echo "Service status:  sudo systemctl status privateDNS"
    echo "View logs:       sudo journalctl -u privateDNS -f"
    echo "Disable service: sudo systemctl disable privateDNS"
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Detected macOS environment"
    
    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root!"
        echo "Run with: sudo $0"
        exit 1
    fi
    
    echo "Installing PrivateDNS as macOS LaunchDaemon..."
    
    # Build the project first
    echo "Building project..."
    cd "$PROJECT_DIR"
    if ! dotnet build -c Release; then
        echo "? Failed to build project"
        exit 1
    fi
    
    # Create LaunchDaemon plist file
    cat > /Library/LaunchDaemons/com.privateDNS.service.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.privateDNS.service</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/local/share/dotnet/dotnet</string>
        <string>$PROJECT_DIR/bin/Release/net8.0/PrivateDNS.dll</string>
    </array>
    <key>WorkingDirectory</key>
    <string>$PROJECT_DIR</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardErrorPath</key>
    <string>/var/log/privateDNS.err</string>
    <key>StandardOutPath</key>
    <string>/var/log/privateDNS.out</string>
</dict>
</plist>
EOF
    
    # Set proper permissions
    chown root:wheel /Library/LaunchDaemons/com.privateDNS.service.plist
    chmod 644 /Library/LaunchDaemons/com.privateDNS.service.plist
    
    # Load the service
    launchctl load /Library/LaunchDaemons/com.privateDNS.service.plist
    
    echo "? LaunchDaemon installed successfully!"
    echo ""
    echo "Service Management Commands:"
    echo "Start service:   sudo launchctl start com.privateDNS.service"
    echo "Stop service:    sudo launchctl stop com.privateDNS.service"
    echo "Unload service:  sudo launchctl unload /Library/LaunchDaemons/com.privateDNS.service.plist"
    echo "View logs:       tail -f /var/log/privateDNS.out"
    
else
    echo "Unsupported operating system: $OSTYPE"
    echo "This script supports Windows, Linux, and macOS"
    exit 1
fi

echo ""
echo "Installation complete!"
echo "Run 'uninstall-service.sh' to remove the service."
echo ""
echo "Note: The service will run on port 5353 by default (non-privileged)."
echo "Use port forwarding scripts if you need standard DNS port 53."