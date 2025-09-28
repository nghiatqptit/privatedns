using System.Net;
using System.Net.Sockets;

namespace PrivateDNS.Services
{
    public class DnsForwarder : IDnsForwarder
    {
        private readonly ILogger<DnsForwarder> _logger;
        private readonly IPEndPoint[] _upstreamServers;

        public DnsForwarder(ILogger<DnsForwarder> logger, IConfiguration configuration)
        {
            _logger = logger;
            
            // Get upstream servers from configuration
            var upstreamServers = configuration.GetSection("DnsProxy:UpstreamDnsServers").Get<string[]>() ?? 
                new[] { "8.8.8.8:53", "8.8.4.4:53", "1.1.1.1:53", "1.0.0.1:53" };
            
            _upstreamServers = upstreamServers.Select(ParseEndpoint).ToArray();
            
            _logger.LogInformation("Configured {Count} upstream DNS servers: {Servers}", 
                _upstreamServers.Length, 
                string.Join(", ", _upstreamServers.Select(s => s.ToString())));
        }

        private static IPEndPoint ParseEndpoint(string endpoint)
        {
            var parts = endpoint.Split(':');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ip) || !int.TryParse(parts[1], out var port))
            {
                throw new ArgumentException($"Invalid endpoint format: {endpoint}. Expected format: IP:PORT");
            }
            return new IPEndPoint(ip, port);
        }

        public async Task<byte[]> ForwardDnsQueryAsync(byte[] query, CancellationToken cancellationToken = default)
        {
            foreach (var server in _upstreamServers)
            {
                try
                {
                    using var udpClient = new UdpClient();
                    udpClient.Client.ReceiveTimeout = 5000;
                    udpClient.Client.SendTimeout = 5000;

                    await udpClient.SendAsync(query, server, cancellationToken);
                    var result = await udpClient.ReceiveAsync(cancellationToken);
                    
                    _logger.LogDebug("DNS query forwarded successfully to {Server}", server);
                    return result.Buffer;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to forward DNS query to {Server}", server);
                }
            }

            throw new InvalidOperationException("All upstream DNS servers failed");
        }
    }
}