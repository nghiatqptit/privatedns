#!/bin/bash

# PrivateDNS Port Forwarding Setup Script
# This script sets up port forwarding from port 53 to 5353
# Requires administrator/root privileges

echo "PrivateDNS Port Forwarding Installer"
echo "===================================="

# Check if running on Windows (Git Bash, WSL, etc.)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    echo "Detected Windows environment"
    
    # Check if running as administrator
    if ! net session > /dev/null 2>&1; then
        echo "ERROR: This script must be run as Administrator!"
        echo "Right-click on Git Bash/Terminal and select 'Run as administrator'"
        exit 1
    fi
    
    echo "Setting up port forwarding: 53 -> 5353"
    
    # Add port forwarding rule
    if netsh interface portproxy add v4tov4 listenport=53 listenaddress=127.0.0.1 connectport=5353 connectaddress=127.0.0.1; then
        echo "? Port forwarding installed successfully!"
        echo ""
        echo "Port 53 (127.0.0.1) -> Port 5353 (127.0.0.1)"
        echo ""
        echo "You can now:"
        echo "1. Start PrivateDNS service: dotnet run"
        echo "2. Configure your DNS to use 127.0.0.1 (standard port 53)"
        echo "3. Test with: nslookup google.com 127.0.0.1"
        echo ""
        echo "To view all port forwarding rules:"
        echo "netsh interface portproxy show all"
    else
        echo "? Failed to install port forwarding"
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
    
    echo "Setting up iptables port forwarding: 53 -> 5353"
    
    # Enable IP forwarding
    echo 1 > /proc/sys/net/ipv4/ip_forward
    
    # Add iptables rules for port forwarding
    iptables -t nat -A OUTPUT -p udp --dport 53 -d 127.0.0.1 -j REDIRECT --to-port 5353
    iptables -t nat -A PREROUTING -p udp --dport 53 -j REDIRECT --to-port 5353
    
    # Save iptables rules (Ubuntu/Debian)
    if command -v iptables-save > /dev/null; then
        iptables-save > /etc/iptables/rules.v4 2>/dev/null || true
    fi
    
    echo "? Port forwarding installed successfully!"
    echo ""
    echo "Port 53 -> Port 5353 (UDP)"
    echo ""
    echo "You can now:"
    echo "1. Start PrivateDNS service: dotnet run"
    echo "2. Configure your DNS to use 127.0.0.1"
    echo "3. Test with: nslookup google.com 127.0.0.1"
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Detected macOS environment"
    
    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root!"
        echo "Run with: sudo $0"
        exit 1
    fi
    
    echo "Setting up pfctl port forwarding: 53 -> 5353"
    
    # Create pf rule file
    cat > /tmp/privateDNS.pf.conf << EOF
rdr on lo0 inet proto udp from any to any port 53 -> 127.0.0.1 port 5353
EOF
    
    # Load the rule
    if pfctl -f /tmp/privateDNS.pf.conf; then
        echo "? Port forwarding installed successfully!"
        echo ""
        echo "Port 53 -> Port 5353 (UDP)"
        echo ""
        echo "You can now:"
        echo "1. Start PrivateDNS service: dotnet run"
        echo "2. Configure your DNS to use 127.0.0.1"
        echo "3. Test with: nslookup google.com 127.0.0.1"
    else
        echo "? Failed to install port forwarding"
        exit 1
    fi
    
else
    echo "Unsupported operating system: $OSTYPE"
    echo "This script supports Windows, Linux, and macOS"
    exit 1
fi

echo ""
echo "Installation complete!"
echo "Run 'uninstall-port-forwarding.sh' to remove port forwarding."