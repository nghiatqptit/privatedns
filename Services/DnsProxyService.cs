using System.Net;
using System.Net.Sockets;
using PrivateDNS.Models;

namespace PrivateDNS.Services
{
    public class DnsProxyService : BackgroundService
    {
        private readonly ILogger<DnsProxyService> _logger;
        private readonly IDnsConfigurationService _configService;
        private readonly IDnsForwarder _dnsForwarder;
        private readonly IConfiguration _configuration;
        private UdpClient? _udpServer;
        private readonly int _dnsPort;

        public DnsProxyService(
            ILogger<DnsProxyService> logger,
            IDnsConfigurationService configService,
            IDnsForwarder dnsForwarder,
            IConfiguration configuration)
        {
            _logger = logger;
            _configService = configService;
            _dnsForwarder = dnsForwarder;
            _configuration = configuration;
            
            // Use configurable port, default to 5353 (non-privileged alternative to 53)
            _dnsPort = _configuration.GetValue<int>("DnsProxy:Port", 5353);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _configService.LoadConfigurationAsync();

            try
            {
                _udpServer = new UdpClient(_dnsPort);
                _logger.LogInformation("DNS Proxy started on port {Port} (non-privileged mode)", _dnsPort);
                _logger.LogInformation("To use this DNS proxy, configure your system to use 127.0.0.1:{Port} as DNS server", _dnsPort);
                
                if (_dnsPort != 53)
                {
                    _logger.LogWarning("Running on non-standard DNS port {Port}. Standard DNS port 53 requires administrator privileges.", _dnsPort);
                    _logger.LogInformation("Alternative setup options:");
                    _logger.LogInformation("1. Use port forwarding: netsh interface portproxy add v4tov4 listenport=53 listenaddress=127.0.0.1 connectport={Port} connectaddress=127.0.0.1", _dnsPort);
                    _logger.LogInformation("2. Configure your network adapter to use 127.0.0.1:{Port} as DNS server", _dnsPort);
                    _logger.LogInformation("3. Use a DNS client that supports custom ports");
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _udpServer.ReceiveAsync(stoppingToken);
                        _ = Task.Run(() => ProcessDnsRequestAsync(result.Buffer, result.RemoteEndPoint, stoppingToken), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error receiving DNS request");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start DNS server on port {Port}", _dnsPort);
            }
        }

        private async Task ProcessDnsRequestAsync(byte[] requestData, IPEndPoint clientEndPoint, CancellationToken cancellationToken)
        {
            try
            {
                var request = DnsMessage.FromBytes(requestData);
                
                if (request.Questions.Count == 0)
                {
                    _logger.LogWarning("Received DNS request with no questions from {Client}", clientEndPoint);
                    return;
                }

                var question = request.Questions[0];
                var domain = question.Name;

                _logger.LogInformation("DNS request for {Domain} from {Client}", domain, clientEndPoint);

                byte[] responseData;

                if (_configService.IsAllowedDomain(domain))
                {
                    _logger.LogInformation("Domain {Domain} is allowed, forwarding to upstream DNS", domain);
                    responseData = await _dnsForwarder.ForwardDnsQueryAsync(requestData, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Domain {Domain} is blocked, returning localhost", domain);
                    responseData = CreateBlockedResponse(request);
                }

                if (_udpServer != null)
                {
                    await _udpServer.SendAsync(responseData, clientEndPoint, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DNS request from {Client}", clientEndPoint);
            }
        }

        private static byte[] CreateBlockedResponse(DnsMessage request)
        {
            var response = new DnsMessage
            {
                TransactionId = request.TransactionId,
                Flags = 0x8180, // Standard query response, no error
                Questions = request.Questions,
                Answers = new List<DnsAnswer>()
            };

            foreach (var question in request.Questions)
            {
                if (question.Type == 1) // A record
                {
                    response.Answers.Add(new DnsAnswer
                    {
                        Name = question.Name,
                        Type = 1, // A record
                        Class = 1, // IN
                        Ttl = 300, // 5 minutes
                        Data = IPAddress.Parse("127.0.0.1").GetAddressBytes()
                    });
                }
            }

            return response.ToBytes();
        }

        public override void Dispose()
        {
            _udpServer?.Dispose();
            base.Dispose();
        }
    }
}