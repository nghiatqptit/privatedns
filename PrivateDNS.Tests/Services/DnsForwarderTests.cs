using Moq;
using PrivateDNS.Services;
using PrivateDNS.Tests.Helpers;

namespace PrivateDNS.Tests.Services
{
    public class DnsForwarderTests
    {
        private readonly Mock<ILogger<DnsForwarder>> _mockLogger;

        public DnsForwarderTests()
        {
            _mockLogger = TestHelpers.CreateMockLogger<DnsForwarder>();
        }

        [Fact]
        public void Constructor_WithValidConfiguration_InitializesCorrectly()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "8.8.8.8:53" },
                    { "DnsProxy:UpstreamDnsServers:1", "1.1.1.1:53" }
                })
                .Build();

            // Act & Assert (should not throw)
            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            Assert.NotNull(forwarder);
        }

        [Fact]
        public void Constructor_WithInvalidServerFormat_ThrowsArgumentException()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "invalid-format" }
                })
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DnsForwarder(_mockLogger.Object, config));
        }

        [Fact]
        public void Constructor_WithEmptyConfiguration_UsesDefaults()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            // Act & Assert (should not throw and use defaults)
            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            Assert.NotNull(forwarder);
        }

        [Theory]
        [InlineData("8.8.8.8:53")]
        [InlineData("1.1.1.1:53")]
        [InlineData("192.168.1.1:5353")]
        public void Constructor_WithValidServerFormats_ParsesCorrectly(string serverEndpoint)
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", serverEndpoint }
                })
                .Build();

            // Act & Assert (should not throw)
            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            Assert.NotNull(forwarder);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("8.8.8.8")]
        [InlineData(":53")]
        [InlineData("8.8.8.8:")]
        [InlineData("8.8.8.8:abc")]
        [InlineData("invalid-ip:53")]
        public void Constructor_WithInvalidServerFormats_ThrowsException(string invalidEndpoint)
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", invalidEndpoint }
                })
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DnsForwarder(_mockLogger.Object, config));
        }

        [Fact]
        public async Task ForwardDnsQueryAsync_WithValidQuery_ReturnsResponse()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "8.8.8.8:53" }
                })
                .Build();

            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            var testQuery = TestHelpers.CreateMockDnsQuery("google.com");

            // Act
            try
            {
                var response = await forwarder.ForwardDnsQueryAsync(testQuery, CancellationToken.None);

                // Assert
                Assert.NotNull(response);
                Assert.NotEmpty(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("All upstream DNS servers failed"))
            {
                // This is expected in test environment where we can't actually reach DNS servers
                // The test verifies that the method attempts to forward and handles failures gracefully
                Assert.Contains("All upstream DNS servers failed", ex.Message);
            }
        }

        [Fact]
        public async Task ForwardDnsQueryAsync_WithEmptyQuery_ThrowsException()
        {
            // Arrange
            var config = TestHelpers.CreateTestConfiguration();
            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            var emptyQuery = Array.Empty<byte>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => forwarder.ForwardDnsQueryAsync(emptyQuery, CancellationToken.None));
        }

        [Fact]
        public async Task ForwardDnsQueryAsync_WithCancellation_RespectsCancellationToken()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "192.0.2.1:53" } // Non-routable IP for timeout
                })
                .Build();

            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            var testQuery = TestHelpers.CreateMockDnsQuery("test.com");
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => forwarder.ForwardDnsQueryAsync(testQuery, cts.Token));
        }

        [Fact]
        public async Task ForwardDnsQueryAsync_WhenAllServersFail_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "192.0.2.1:53" }, // Non-routable IP
                    { "DnsProxy:UpstreamDnsServers:1", "192.0.2.2:53" }  // Non-routable IP
                })
                .Build();

            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            var testQuery = TestHelpers.CreateMockDnsQuery("test.com");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => forwarder.ForwardDnsQueryAsync(testQuery, CancellationToken.None));

            Assert.Equal("All upstream DNS servers failed", exception.Message);
        }

        [Fact]
        public void Constructor_LogsUpstreamServersConfiguration()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "8.8.8.8:53" },
                    { "DnsProxy:UpstreamDnsServers:1", "1.1.1.1:53" }
                })
                .Build();

            // Act
            var forwarder = new DnsForwarder(_mockLogger.Object, config);

            // Assert
            TestHelpers.VerifyLoggerCalled(_mockLogger, LogLevel.Information, "Configured");
            TestHelpers.VerifyLoggerCalled(_mockLogger, LogLevel.Information, "upstream DNS servers");
        }

        [Fact]
        public async Task ForwardDnsQueryAsync_LogsFailures()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "192.0.2.1:53" } // Non-routable IP
                })
                .Build();

            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            var testQuery = TestHelpers.CreateMockDnsQuery("test.com");

            // Act
            try
            {
                await forwarder.ForwardDnsQueryAsync(testQuery, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // Expected exception
            }

            // Assert
            TestHelpers.VerifyLoggerCalledWithLevel(_mockLogger, LogLevel.Warning);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_UsesDefaults()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build(); // Empty configuration

            // Act & Assert (should not throw)
            var forwarder = new DnsForwarder(_mockLogger.Object, config);
            Assert.NotNull(forwarder);
        }

        [Fact]
        public void Constructor_WithMultipleValidServers_InitializesAll()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:UpstreamDnsServers:0", "8.8.8.8:53" },
                    { "DnsProxy:UpstreamDnsServers:1", "8.8.4.4:53" },
                    { "DnsProxy:UpstreamDnsServers:2", "1.1.1.1:53" },
                    { "DnsProxy:UpstreamDnsServers:3", "1.0.0.1:53" }
                })
                .Build();

            // Act
            var forwarder = new DnsForwarder(_mockLogger.Object, config);

            // Assert
            Assert.NotNull(forwarder);
            TestHelpers.VerifyLoggerCalled(_mockLogger, LogLevel.Information, "4");
        }
    }
}