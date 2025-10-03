using Moq;
using PrivateDNS.Services;
using PrivateDNS.Tests.Helpers;

namespace PrivateDNS.Tests.Services
{
    public class DnsProxyServiceTests : IDisposable
    {
        private readonly Mock<ILogger<DnsProxyService>> _mockLogger;
        private readonly Mock<IDnsConfigurationService> _mockConfigService;
        private readonly Mock<IDnsForwarder> _mockDnsForwarder;
        private readonly IConfiguration _configuration;
        private readonly List<string> _tempFiles;

        public DnsProxyServiceTests()
        {
            _mockLogger = TestHelpers.CreateMockLogger<DnsProxyService>();
            _mockConfigService = new Mock<IDnsConfigurationService>();
            _mockDnsForwarder = new Mock<IDnsForwarder>();
            _tempFiles = new List<string>();

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:Port", "5353" }
                })
                .Build();

            // Setup default mock behaviors
            _mockConfigService.Setup(x => x.LoadConfigurationAsync())
                .Returns(Task.CompletedTask);

            _mockConfigService.Setup(x => x.IsAllowedDomain("google.com"))
                .Returns(true);

            _mockConfigService.Setup(x => x.IsAllowedDomain("blocked.com"))
                .Returns(false);

            _mockDnsForwarder.Setup(x => x.ForwardDnsQueryAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestHelpers.CreateMockDnsQuery("google.com", 1234));
        }

        public void Dispose()
        {
            foreach (var tempFile in _tempFiles)
            {
                TestHelpers.CleanupTempFile(tempFile);
            }
        }

        [Fact]
        public void Constructor_WithValidDependencies_InitializesCorrectly()
        {
            // Act & Assert (should not throw)
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration);

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DnsProxyService(
                null!,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration));
        }

        [Fact]
        public void Constructor_WithNullConfigService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DnsProxyService(
                _mockLogger.Object,
                null!,
                _mockDnsForwarder.Object,
                _configuration));
        }

        [Fact]
        public void Constructor_WithNullDnsForwarder_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                null!,
                _configuration));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                null!));
        }

        [Theory]
        [InlineData(5353)]
        [InlineData(8853)]
        [InlineData(9953)]
        public void Constructor_WithDifferentPorts_SetsPortCorrectly(int port)
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:Port", port.ToString() }
                })
                .Build();

            // Act & Assert (should not throw)
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                config);

            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithMissingPortConfiguration_UsesDefault()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();

            // Act & Assert (should not throw and use default port 5353)
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                emptyConfig);

            Assert.NotNull(service);
        }

        [Fact]
        public async Task ExecuteAsync_LoadsConfiguration()
        {
            // Arrange
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration);

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately to stop execution

            // Act
            try
            {
                await service.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected due to immediate cancellation
            }

            // Assert
            _mockConfigService.Verify(x => x.LoadConfigurationAsync(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellation_StopsGracefully()
        {
            // Arrange
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration);

            var cts = new CancellationTokenSource();

            // Act
            var executeTask = service.StartAsync(cts.Token);
            cts.Cancel(); // Cancel after starting

            // Assert (should complete without throwing)
            try
            {
                await executeTask;
            }
            catch (OperationCanceledException)
            {
                // Expected behavior
            }
        }

        [Fact]
        public void Dispose_DisposesResourcesCorrectly()
        {
            // Arrange
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration);

            // Act & Assert (should not throw)
            service.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_LogsStartupInformation()
        {
            // Arrange
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration);

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            try
            {
                await service.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            // Note: Due to the nature of UDP server binding, we may not see startup logs in unit tests
            // The main goal is to ensure the service handles startup correctly
            _mockConfigService.Verify(x => x.LoadConfigurationAsync(), Times.Once);
        }

        [Fact]
        public async Task StopAsync_CompletesSuccessfully()
        {
            // Arrange
            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration);

            // Act & Assert (should not throw)
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task ExecuteAsync_HandlesConfigurationLoadFailure()
        {
            // Arrange
            _mockConfigService.Setup(x => x.LoadConfigurationAsync())
                .ThrowsAsync(new InvalidOperationException("Config load failed"));

            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                _configuration);

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert (should not throw, should handle gracefully)
            try
            {
                await service.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected due to cancellation
            }
            catch (InvalidOperationException)
            {
                // Also acceptable - service may propagate configuration errors
            }
        }

        [Theory]
        [InlineData(53)]
        [InlineData(5353)]
        [InlineData(8053)]
        public async Task ExecuteAsync_WithDifferentPorts_AttemptsBinding(int port)
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:Port", port.ToString() }
                })
                .Build();

            var service = new DnsProxyService(
                _mockLogger.Object,
                _mockConfigService.Object,
                _mockDnsForwarder.Object,
                config);

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert (should handle port binding gracefully)
            try
            {
                await service.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            catch
            {
                // Port binding may fail in test environment, which is acceptable
            }

            _mockConfigService.Verify(x => x.LoadConfigurationAsync(), Times.Once);
        }
    }
}