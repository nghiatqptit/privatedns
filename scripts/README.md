# PrivateDNS Management Scripts

This folder contains all the management scripts for PrivateDNS proxy service.

## ?? Quick Setup

### One-Command Complete Setup

**Windows (Run as Administrator):**
```cmd
cd scripts
setup-wizard.bat
```

**Linux/macOS:**
```bash
chmod +x scripts/*.sh
sudo ./scripts/setup-wizard.sh
```

The setup wizard will:
1. Build the PrivateDNS project
2. Install as system service
3. Optionally set up port forwarding
4. Verify configuration files
5. Start the service

## ?? Script Organization

### Setup Wizards
| Script | Platform | Purpose |
|--------|----------|---------|
| `setup-wizard.sh` | Linux/macOS/Git Bash | Complete automated setup |
| `setup-wizard.bat` | Windows | Complete automated setup |

### Port Forwarding Scripts
| Script | Platform | Purpose |
|--------|----------|---------|
| `install-port-forwarding.sh` | Linux/macOS/Git Bash | Install port forwarding (53?5353) |
| `install-port-forwarding.bat` | Windows | Install port forwarding (53?5353) |
| `uninstall-port-forwarding.sh` | Linux/macOS/Git Bash | Remove port forwarding |
| `uninstall-port-forwarding.bat` | Windows | Remove port forwarding |
| `check-port-forwarding.sh` | Linux/macOS/Git Bash | Check port forwarding status |
| `check-port-forwarding.bat` | Windows | Check port forwarding status |

### Service Management Scripts
| Script | Platform | Purpose |
|--------|----------|---------|
| `install-service.sh` | Linux/macOS/Git Bash | Install PrivateDNS as system service |
| `install-service.bat` | Windows | Install PrivateDNS as Windows Service |
| `uninstall-service.sh` | Linux/macOS/Git Bash | Remove PrivateDNS system service |
| `uninstall-service.bat` | Windows | Remove PrivateDNS Windows Service |
| `check-service.sh` | Linux/macOS/Git Bash | Check service status |
| `check-service.bat` | Windows | Check service status |

## ?? Recommended Workflow

### For Production Deployment
1. **Complete Setup**: Use `setup-wizard.sh/.bat` for automated installation
2. **Configure Domains**: Edit `allowed-domains.json` for your requirements
3. **Test**: Verify DNS resolution with test domains
4. **Monitor**: Use check scripts to monitor status

### For Development
1. **Direct Run**: Use `dotnet run` for development testing
2. **Custom Port**: Test with `nslookup domain 127.0.0.1:5353`
3. **Service Install**: Use individual scripts for specific components

## ?? Script Details

### Setup Wizard Scripts

The setup wizards provide a complete, interactive installation experience:

**Features:**
- ? Platform detection and privilege verification
- ? Project building and validation
- ? Service installation with error handling
- ? Interactive port forwarding setup
- ? Configuration file verification
- ? Service startup and status checking
- ? Comprehensive setup summary

**What the wizard does:**
1. Detects platform (Windows/Linux/macOS) and verifies admin privileges
2. Builds the PrivateDNS project in Release configuration
3. Installs PrivateDNS as a system service
4. Asks if you want port forwarding (optional)
5. Verifies configuration files exist
6. Offers to start the service immediately
7. Provides next steps and management commands

### Port Forwarding Scripts

Port forwarding allows PrivateDNS to receive DNS requests on the standard port 53 while running on non-privileged port 5353.

**Features:**
- ? Cross-platform support (Windows, Linux, macOS)
- ? Administrator/root privilege checking
- ? Automatic platform detection
- ? Safe install/uninstall process
- ? Status validation

**Windows Implementation:**
- Uses `netsh interface portproxy` commands
- Forwards TCP/UDP port 53 to 5353

**Linux Implementation:**
- Uses `iptables` NAT rules
- Redirects UDP port 53 to 5353
- Saves rules to persist across reboots

**macOS Implementation:**
- Uses `pfctl` packet filter rules
- Redirects UDP port 53 to 5353

### Service Management Scripts

Service scripts install PrivateDNS as a system service that starts automatically with the system.

**Features:**
- ? Cross-platform service installation
- ? Automatic startup configuration
- ? Service restart on failure
- ? Proper working directory setup
- ? Logging configuration

**Windows Implementation:**
- Creates Windows Service using `sc` command
- Service runs as LocalSystem account
- Configurable restart policy

**Linux Implementation:**
- Creates systemd service unit
- Runs as root for port binding
- Journal logging integration

**macOS Implementation:**
- Creates LaunchDaemon plist
- Runs as root daemon
- Automatic restart on failure

## ?? Usage Examples

### Complete Setup (Recommended)

```bash
# Linux/macOS - Complete setup with wizard
chmod +x scripts/*.sh
sudo ./scripts/setup-wizard.sh
```

```cmd
REM Windows - Complete setup with wizard (Run as Admin)
scripts\setup-wizard.bat
```

### Individual Component Management

```bash
# Install service only
sudo ./scripts/install-service.sh

# Install port forwarding only
sudo ./scripts/install-port-forwarding.sh

# Check status
./scripts/check-service.sh
./scripts/check-port-forwarding.sh

# Start/stop service (Linux)
sudo systemctl start privateDNS
sudo systemctl stop privateDNS

# View logs (Linux)
sudo journalctl -u privateDNS -f
```

```cmd
REM Install service only (Windows - Run as Admin)
scripts\install-service.bat

REM Install port forwarding only
scripts\install-port-forwarding.bat

REM Check status
scripts\check-service.bat
scripts\check-port-forwarding.bat

REM Start/stop service
sc start PrivateDNS
sc stop PrivateDNS
```

## ??? Troubleshooting

### Common Issues

1. **Permission Denied**: Make sure to run scripts as administrator/root
2. **Service Won't Start**: Check if .NET 8 runtime is installed
3. **Port Already in Use**: Stop other DNS services or change port in config
4. **Scripts Not Executable**: Run `chmod +x scripts/*.sh`
5. **Build Failures**: Ensure .NET 8 SDK is installed

### Platform-Specific Issues

**Windows:**
- Must run Command Prompt or PowerShell as Administrator
- Windows Defender might block service creation
- Check Windows Firewall settings
- Ensure .NET 8 runtime is installed

**Linux:**
- systemd must be available
- iptables must be installed for port forwarding
- Check SELinux policies if enabled
- Verify .NET 8 runtime installation

**macOS:**
- SIP (System Integrity Protection) might interfere
- pfctl rules require root privileges
- Check for conflicting DNS services
- Install .NET 8 runtime if needed

### Log Locations

**Windows Service:**
- Event Viewer ? Windows Logs ? Application
- Filter by Source: PrivateDNS

**Linux systemd:**
```bash
sudo journalctl -u privateDNS -f
```

**macOS LaunchDaemon:**
```bash
tail -f /var/log/privateDNS.out
tail -f /var/log/privateDNS.err
```

### Setup Wizard Troubleshooting

**Build Failures:**
- Ensure .NET 8 SDK is installed: `dotnet --version`
- Check project files are not corrupted
- Verify network connectivity for package restore

**Service Installation Failures:**
- Verify administrator/root privileges
- Check if service name conflicts with existing services
- Ensure target directories are writable

**Port Forwarding Issues:**
- Check if port 53 is already in use
- Verify firewall settings allow DNS traffic
- Test with netstat/ss to confirm port binding

## ?? Security Notes

- Service scripts require administrator/root privileges for installation
- Port forwarding requires elevated privileges to bind to port 53
- Services run with system privileges for port binding
- Setup wizard includes comprehensive privilege checking
- Always verify script integrity before running with elevated privileges

## ?? Support

If you encounter issues:
1. Run the setup wizard first - it handles most common scenarios
2. Check the troubleshooting section above
3. Verify system requirements (.NET 8 runtime)
4. Check firewall and DNS service conflicts
5. Review the main README.md for configuration options

For detailed logging, enable debug mode in `appsettings.Development.json` and review service logs.