#!/bin/bash

# PrivateDNS Port Forwarding Uninstall Script
# This script removes port forwarding from port 53 to 5353
# Requires administrator/root privileges

echo "PrivateDNS Port Forwarding Uninstaller"
echo "======================================"

# Check if running on Windows (Git Bash, WSL, etc.)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    echo "Detected Windows environment"
    
    # Check if running as administrator
    if ! net session > /dev/null 2>&1; then
        echo "ERROR: This script must be run as Administrator!"
        echo "Right-click on Git Bash/Terminal and select 'Run as administrator'"
        exit 1
    fi
    
    echo "Removing port forwarding: 53 -> 5353"
    
    # Remove port forwarding rule
    if netsh interface portproxy delete v4tov4 listenport=53 listenaddress=127.0.0.1; then
        echo "? Port forwarding removed successfully!"
        echo ""
        echo "To view remaining port forwarding rules:"
        echo "netsh interface portproxy show all"
    else
        echo "? No port forwarding rule found or failed to remove"
        echo "This might be normal if no rule was previously installed"
    fi
    
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Detected Linux environment"
    
    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root!"
        echo "Run with: sudo $0"
        exit 1
    fi
    
    echo "Removing iptables port forwarding: 53 -> 5353"
    
    # Remove iptables rules for port forwarding
    iptables -t nat -D OUTPUT -p udp --dport 53 -d 127.0.0.1 -j REDIRECT --to-port 5353 2>/dev/null || echo "? OUTPUT rule not found"
    iptables -t nat -D PREROUTING -p udp --dport 53 -j REDIRECT --to-port 5353 2>/dev/null || echo "? PREROUTING rule not found"
    
    # Save iptables rules (Ubuntu/Debian)
    if command -v iptables-save > /dev/null; then
        iptables-save > /etc/iptables/rules.v4 2>/dev/null || true
    fi
    
    echo "? Port forwarding rules removed!"
    echo ""
    echo "To view remaining iptables rules:"
    echo "iptables -t nat -L"
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Detected macOS environment"
    
    # Check if running as root
    if [[ $EUID -ne 0 ]]; then
        echo "ERROR: This script must be run as root!"
        echo "Run with: sudo $0"
        exit 1
    fi
    
    echo "Removing pfctl port forwarding: 53 -> 5353"
    
    # Flush pf rules (this removes all pf rules, might be too aggressive)
    echo "? This will flush all pfctl rules"
    read -p "Continue? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        if pfctl -F all; then
            echo "? All pfctl rules flushed!"
        else
            echo "? Failed to flush pfctl rules"
            exit 1
        fi
    else
        echo "Operation cancelled"
        echo "Manual removal: pfctl -F all"
    fi
    
else
    echo "Unsupported operating system: $OSTYPE"
    echo "This script supports Windows, Linux, and macOS"
    exit 1
fi

echo ""
echo "Uninstall complete!"
echo "You can now use PrivateDNS with direct port configuration (127.0.0.1:5353)"