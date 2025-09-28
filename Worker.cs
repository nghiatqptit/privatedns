namespace PrivateDNS
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PrivateDNS Worker Service started at: {time}", DateTimeOffset.Now);
            
            // The DNS proxy functionality is now handled by DnsProxyService
            // This worker can be used for additional monitoring or management tasks
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Worker heartbeat at: {time}", DateTimeOffset.Now);
                await Task.Delay(30000, stoppingToken); // Log every 30 seconds
            }
            
            _logger.LogInformation("PrivateDNS Worker Service stopped at: {time}", DateTimeOffset.Now);
        }
    }
}
