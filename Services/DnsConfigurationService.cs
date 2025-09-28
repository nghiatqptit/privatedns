using System.Text.Json;

namespace PrivateDNS.Services
{
    public class DnsConfigurationService : IDnsConfigurationService
    {
        private readonly ILogger<DnsConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HashSet<string> _allowedDomains;
        private readonly string _configFilePath;

        public DnsConfigurationService(ILogger<DnsConfigurationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _allowedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Get config file path from configuration
            var configFileName = _configuration["DnsProxy:AllowedDomainsFile"] ?? "allowed-domains.json";
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);
        }

        public bool IsAllowedDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            // Remove trailing dot if present
            domain = domain.TrimEnd('.');

            // Check exact match
            if (_allowedDomains.Contains(domain))
                return true;

            // Check if any parent domain is allowed (wildcard support)
            var parts = domain.Split('.');
            for (int i = 1; i < parts.Length; i++)
            {
                var parentDomain = string.Join(".", parts.Skip(i));
                if (_allowedDomains.Contains($"*.{parentDomain}"))
                    return true;
            }

            return false;
        }

        public async Task LoadConfigurationAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    var domains = JsonSerializer.Deserialize<string[]>(json);
                    
                    if (domains != null)
                    {
                        _allowedDomains.Clear();
                        foreach (var domain in domains)
                        {
                            _allowedDomains.Add(domain.TrimEnd('.'));
                        }
                        _logger.LogInformation("Loaded {Count} allowed domains from {ConfigFile}", _allowedDomains.Count, _configFilePath);
                    }
                }
                else
                {
                    // Create default configuration
                    var defaultDomains = new[]
                    {
                        "google.com",
                        "*.google.com",
                        "microsoft.com",
                        "*.microsoft.com",
                        "github.com",
                        "*.github.com"
                    };

                    foreach (var domain in defaultDomains)
                    {
                        _allowedDomains.Add(domain);
                    }

                    await SaveConfigurationAsync();
                    _logger.LogInformation("Created default configuration with {Count} domains at {ConfigFile}", _allowedDomains.Count, _configFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load DNS configuration from {ConfigFile}", _configFilePath);
            }
        }

        public void AddAllowedDomain(string domain)
        {
            if (!string.IsNullOrWhiteSpace(domain))
            {
                _allowedDomains.Add(domain.TrimEnd('.'));
                _ = SaveConfigurationAsync();
            }
        }

        public void RemoveAllowedDomain(string domain)
        {
            if (!string.IsNullOrWhiteSpace(domain))
            {
                _allowedDomains.Remove(domain.TrimEnd('.'));
                _ = SaveConfigurationAsync();
            }
        }

        public IEnumerable<string> GetAllowedDomains()
        {
            return _allowedDomains.ToList();
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_allowedDomains.ToArray(), new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save DNS configuration to {ConfigFile}", _configFilePath);
            }
        }
    }
}