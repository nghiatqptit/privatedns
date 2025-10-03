# PrivateDNS Proxy

A DNS proxy server that filters domain requests based on a whitelist configuration.

## Features

- **Non-privileged operation**: Runs on port 5353 by default (no administrator rights required)
- **Domain whitelisting**: Only allows configured domains, blocks others by returning 127.0.0.1
- **Wildcard support**: Use `*.domain.com` to allow all subdomains
- **Configurable upstream DNS servers**: Forward allowed requests to multiple DNS providers
- **JSON configuration**: Easy to modify allowed domains list
- **Cross-platform management scripts**: Automated setup for Windows, Linux, and macOS
- **System service support**: Install as Windows Service, systemd service, or macOS LaunchDaemon
- **Comprehensive unit testing**: Full test coverage with performance benchmarks

## Quick Start

### 1. Run Directly (Development/Testing)
```cmd
# Clone and run
dotnet run

# Test with custom port
nslookup google.com 127.0.0.1:5353
```

### 2. Install as System Service (Production)

**Windows (Run as Administrator):**
```cmd
cd scripts
install-service.bat
```

**Linux/macOS:**
```bash
# Make scripts executable
chmod +x scripts/*.sh

# Install service
sudo ./scripts/install-service.sh
```

### 3. Optional: Set up Standard DNS Port (53)

**Windows (Run as Administrator):**
```cmd
cd scripts
install-port-forwarding.bat
```

**Linux/macOS:**
```bash
sudo ./scripts/install-port-forwarding.sh
```

## Testing

The project includes comprehensive unit tests covering all major components.

### Running Tests

**Using Test Scripts:**
```bash
# Linux/macOS/Git Bash
chmod +x scripts/run-tests.sh
./scripts/run-tests.sh

# Windows
scripts\run-tests.bat
```

**Using dotnet CLI:**
```cmd
# Run all tests
dotnet test PrivateDNS.Tests

# Run with coverage
dotnet test PrivateDNS.Tests --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test PrivateDNS.Tests --filter "Category=Unit"
```

### Test Coverage

The test suite includes:

- **Unit Tests**: Individual component testing
  - DNS Message parsing and serialization
  - Domain configuration and matching logic
  - DNS forwarding functionality
  - Service lifecycle management

- **Integration Tests**: End-to-end functionality
  - Service registration and dependency injection
  - Configuration loading and validation
  - Full DNS processing pipeline

- **Performance Tests**: Benchmarking and optimization
  - DNS message processing speed
  - Domain matching performance with large lists
  - Memory usage optimization
  - Concurrent access testing

## Configuration

### DNS Proxy Settings (appsettings.json)

```json
{
  "DnsProxy": {
    "Port": 5353,
    "UpstreamDnsServers": [
      "8.8.8.8:53",
      "8.8.4.4:53",
      "1.1.1.1:53",
      "1.0.0.1:53"
    ],
    "AllowedDomainsFile": "allowed-domains.json"
  }
}
```

### Allowed Domains (allowed-domains.json)

```json
[
  "google.com",
  "*.google.com",
  "microsoft.com",
  "*.microsoft.com",
  "github.com",
  "*.github.com"
]
```

## Usage Options

### Option 1: Direct DNS Client Configuration
Configure your applications or system to use `127.0.0.1:5353` as the DNS server.

**For testing with nslookup:**
```cmd
nslookup google.com 127.0.0.1:5353
```

### Option 2: System Service with Port Forwarding (Recommended for Production)

Use the automated management scripts in the `scripts/` folder:

**Windows:**
```cmd
# Install as Windows Service
scripts\install-service.bat

# Set up port forwarding (optional)
scripts\install-port-forwarding.bat

# Check status
scripts\check-service.bat
scripts\check-port-forwarding.bat
```

**Linux/macOS:**
```bash
# Install as system service
sudo ./scripts/install-service.sh

# Set up port forwarding (optional)
sudo ./scripts/install-port-forwarding.sh

# Check status
./scripts/check-service.sh
./scripts/check-port-forwarding.sh
```

### Option 3: Manual Development Mode
Change the port to 53 in `appsettings.json` and run as administrator:

```json
{
  "DnsProxy": {
    "Port": 53
  }
}
```

## Management Scripts

The `scripts/` folder contains comprehensive management tools:

### Service Management
| Script | Platform | Purpose |
|--------|----------|---------|
| `install-service.sh/.bat` | Cross-platform | Install PrivateDNS as system service |
| `uninstall-service.sh/.bat` | Cross-platform | Remove PrivateDNS system service |
| `check-service.sh/.bat` | Cross-platform | Check service status |

### Port Forwarding
| Script | Platform | Purpose |
|--------|----------|---------|
| `install-port-forwarding.sh/.bat` | Cross-platform | Install port forwarding (53?5353) |
| `uninstall-port-forwarding.sh/.bat` | Cross-platform | Remove port forwarding |
| `check-port-forwarding.sh/.bat` | Cross-platform | Check port forwarding status |

### Testing and Development
| Script | Platform | Purpose |
|--------|----------|---------|
| `run-tests.sh/.bat` | Cross-platform | Run unit tests with coverage |
| `setup-github.sh/.bat` | Cross-platform | Setup Git repository for GitHub |

**Features:**
- ? Cross-platform support (Windows, Linux, macOS)
- ? Administrator/root privilege checking
- ? Error handling and status reporting
- ? Safe install/uninstall processes
- ? Service auto-start configuration
- ? Comprehensive documentation

See `scripts/README.md` for detailed script documentation.

## Service Management Commands

### Windows Service
```cmd
# Manual service control
sc start PrivateDNS
sc stop PrivateDNS
sc query PrivateDNS

# Or use Services.msc GUI
```

### Linux systemd
```bash
# Service control
sudo systemctl start privateDNS
sudo systemctl stop privateDNS
sudo systemctl status privateDNS

# View logs
sudo journalctl -u privateDNS -f
```

### macOS LaunchDaemon
```bash
# Service control
sudo launchctl start com.privateDNS.service
sudo launchctl stop com.privateDNS.service

# View logs
tail -f /var/log/privateDNS.out
```

## Logging

The application provides detailed logging showing:
- DNS requests received
- Domain allow/block decisions
- Upstream DNS forwarding
- Configuration loading
- Service lifecycle events

Set log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "PrivateDNS": "Information"
    }
  }
}
```

## Troubleshooting

### Common Issues

1. **"Permission denied" or "Access denied"**: Run scripts as administrator/root
2. **"Port already in use"**: Stop other DNS services or use a different port
3. **DNS not resolving**: Check firewall settings and DNS configuration
4. **Scripts not executable**: Run `chmod +x scripts/*.sh` on the script files
5. **Service won't start**: Verify .NET 8 runtime is installed
6. **Tests failing**: Ensure all NuGet packages are restored

### Platform-Specific Issues

**Windows:**
- Service requires .NET 8 runtime
- Windows Firewall might block DNS traffic
- Use Services.msc for GUI management

**Linux:**
- Requires systemd for service management
- iptables needed for port forwarding
- Check for conflicting DNS services (systemd-resolved, dnsmasq)

**macOS:**
- SIP might interfere with system modifications
- pfctl requires root privileges
- Check for conflicting DNS services

### Log Locations

- **Windows Service**: Event Viewer ? Application logs
- **Linux systemd**: `sudo journalctl -u privateDNS -f`
- **macOS LaunchDaemon**: `/var/log/privateDNS.out`

## Development

### Project Structure

```
PrivateDNS/
??? Services/
?   ??? DnsProxyService.cs      # Main DNS proxy logic
?   ??? DnsConfigurationService.cs # Domain whitelist management
?   ??? DnsForwarder.cs         # Upstream DNS forwarding
??? Models/
?   ??? DnsMessage.cs           # DNS protocol handling
??? PrivateDNS.Tests/           # Unit test project
?   ??? Models/                 # Model tests
?   ??? Services/               # Service tests
?   ??? Integration/            # Integration tests
?   ??? Performance/            # Performance tests
?   ??? TestData/              # Test configuration files
??? scripts/                    # Management scripts
?   ??? install-service.*       # Service installation
?   ??? install-port-forwarding.* # Port forwarding setup
?   ??? run-tests.*            # Test execution
?   ??? setup-github.*         # Git repository setup
??? appsettings.json           # Configuration
??? allowed-domains.json       # Domain whitelist
??? README.md                  # This file
```

### Building from Source

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/PrivateDNS.git
cd PrivateDNS

# Restore packages
dotnet restore

# Build the project
dotnet build --configuration Release

# Run tests
./scripts/run-tests.sh  # or run-tests.bat on Windows

# Run the application
dotnet run
```

## Security Notes

- The proxy blocks non-whitelisted domains by returning localhost (127.0.0.1)
- All allowed domains are forwarded to configured upstream DNS servers
- Configuration files can be modified at runtime
- Service installation requires administrator privileges
- Port forwarding requires one-time administrator setup
- Scripts include comprehensive privilege and safety checks