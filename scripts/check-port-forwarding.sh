#!/bin/bash

# PrivateDNS Port Forwarding Status Checker
# This script checks the current port forwarding status

echo "PrivateDNS Port Forwarding Status"
echo "================================="

# Check if running on Windows (Git Bash, WSL, etc.)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    echo "Platform: Windows"
    echo ""
    echo "Current port forwarding rules:"
    netsh interface portproxy show all
    echo ""
    
    # Check if our specific rule exists
    if netsh interface portproxy show all | grep -q "53.*127.0.0.1.*5353"; then
        echo "? PrivateDNS port forwarding is ACTIVE (53 -> 5353)"
    else
        echo "? PrivateDNS port forwarding is NOT configured"
    fi
    
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Platform: Linux"
    echo ""
    echo "Current iptables NAT rules:"
    iptables -t nat -L -n --line-numbers 2>/dev/null || echo "Run with sudo to see iptables rules"
    echo ""
    
    # Check if our specific rules exist
    if iptables -t nat -L -n 2>/dev/null | grep -q "REDIRECT.*udp.*dpt:53.*redir :5353"; then
        echo "? PrivateDNS port forwarding is ACTIVE (53 -> 5353)"
    else
        echo "? PrivateDNS port forwarding is NOT configured"
    fi
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Platform: macOS"
    echo ""
    echo "Current pfctl NAT rules:"
    pfctl -s nat 2>/dev/null || echo "Run with sudo to see pfctl rules"
    echo ""
    
    # Check if our specific rule exists
    if pfctl -s nat 2>/dev/null | grep -q "rdr.*port 53.*port 5353"; then
        echo "? PrivateDNS port forwarding is ACTIVE (53 -> 5353)"
    else
        echo "? PrivateDNS port forwarding is NOT configured"
    fi
    
else
    echo "Platform: $OSTYPE (unsupported)"
    echo "This script supports Windows, Linux, and macOS"
fi

echo ""
echo "To install port forwarding: ./install-port-forwarding.sh"
echo "To remove port forwarding:  ./uninstall-port-forwarding.sh"