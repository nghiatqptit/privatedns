using PrivateDNS;
using PrivateDNS.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<IDnsConfigurationService, DnsConfigurationService>();
builder.Services.AddSingleton<IDnsForwarder, DnsForwarder>();

// Register hosted services
builder.Services.AddHostedService<DnsProxyService>();
builder.Services.AddHostedService<Worker>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var configuration = host.Services.GetRequiredService<IConfiguration>();
var dnsPort = configuration.GetValue<int>("DnsProxy:Port", 5353);

logger.LogInformation("Starting PrivateDNS Proxy Server on port {Port}...", dnsPort);

if (dnsPort == 53)
{
    logger.LogWarning("Using standard DNS port 53 - this requires administrator privileges");
}
else
{
    logger.LogInformation("Running in non-privileged mode on port {Port}", dnsPort);
    logger.LogInformation("Setup instructions:");
    logger.LogInformation("1. Configure your DNS client to use 127.0.0.1:{Port}", dnsPort);
    logger.LogInformation("2. Or use port forwarding (requires admin once): netsh interface portproxy add v4tov4 listenport=53 listenaddress=127.0.0.1 connectport={Port} connectaddress=127.0.0.1", dnsPort);
}

host.Run();
