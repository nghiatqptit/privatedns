using Moq;
using PrivateDNS.Services;
using PrivateDNS.Tests.Helpers;

namespace PrivateDNS.Tests.Services
{
    public class DnsConfigurationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<DnsConfigurationService>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly List<string> _tempFiles;

        public DnsConfigurationServiceTests()
        {
            _mockLogger = TestHelpers.CreateMockLogger<DnsConfigurationService>();
            _configuration = TestHelpers.CreateTestConfiguration();
            _tempFiles = new List<string>();
        }

        public void Dispose()
        {
            foreach (var tempFile in _tempFiles)
            {
                TestHelpers.CleanupTempFile(tempFile);
            }
        }

        [Fact]
        public async Task LoadConfigurationAsync_WithExistingFile_LoadsDomains()
        {
            // Arrange
            var testDomains = new[] { "test1.com", "test2.com", "*.test3.com" };
            var tempFile = TestHelpers.CreateTempConfigFile(testDomains);
            _tempFiles.Add(tempFile);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:AllowedDomainsFile", tempFile }
                })
                .Build();

            var service = new DnsConfigurationService(_mockLogger.Object, config);

            // Act
            await service.LoadConfigurationAsync();

            // Assert
            Assert.True(service.IsAllowedDomain("test1.com"));
            Assert.True(service.IsAllowedDomain("test2.com"));
            Assert.True(service.IsAllowedDomain("sub.test3.com"));
            Assert.False(service.IsAllowedDomain("notallowed.com"));
        }

        [Fact]
        public async Task LoadConfigurationAsync_WithNonExistentFile_CreatesDefaultConfig()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:AllowedDomainsFile", nonExistentFile }
                })
                .Build();

            var service = new DnsConfigurationService(_mockLogger.Object, config);

            // Act
            await service.LoadConfigurationAsync();

            // Assert
            Assert.True(service.IsAllowedDomain("google.com"));
            Assert.True(service.IsAllowedDomain("microsoft.com"));
            Assert.True(service.IsAllowedDomain("github.com"));

            // Cleanup
            TestHelpers.CleanupTempFile(nonExistentFile);
        }

        [Theory]
        [InlineData("google.com", true)]
        [InlineData("www.google.com", true)] // Wildcard match
        [InlineData("mail.google.com", true)] // Wildcard match
        [InlineData("notallowed.com", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsAllowedDomain_WithVariousDomains_ReturnsExpectedResult(string? domain, bool expected)
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            
            // Simulate loading test domains
            service.AddAllowedDomain("google.com");
            service.AddAllowedDomain("*.google.com");

            // Act
            var result = service.IsAllowedDomain(domain!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsAllowedDomain_WithTrailingDot_HandlesCorrectly()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            service.AddAllowedDomain("example.com");

            // Act & Assert
            Assert.True(service.IsAllowedDomain("example.com."));
            Assert.True(service.IsAllowedDomain("example.com"));
        }

        [Fact]
        public void IsAllowedDomain_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            service.AddAllowedDomain("Example.COM");

            // Act & Assert
            Assert.True(service.IsAllowedDomain("example.com"));
            Assert.True(service.IsAllowedDomain("EXAMPLE.COM"));
            Assert.True(service.IsAllowedDomain("Example.Com"));
        }

        [Theory]
        [InlineData("*.example.com", "sub.example.com", true)]
        [InlineData("*.example.com", "deep.sub.example.com", true)]
        [InlineData("*.example.com", "example.com", false)] // Wildcard doesn't match exact domain
        [InlineData("*.example.com", "notexample.com", false)]
        public void IsAllowedDomain_WithWildcards_HandlesCorrectly(string allowedPattern, string testDomain, bool expected)
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            service.AddAllowedDomain(allowedPattern);

            // Act
            var result = service.IsAllowedDomain(testDomain);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void AddAllowedDomain_WithValidDomain_AddsDomain()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);

            // Act
            service.AddAllowedDomain("newdomain.com");

            // Assert
            Assert.True(service.IsAllowedDomain("newdomain.com"));
        }

        [Fact]
        public void AddAllowedDomain_WithEmptyDomain_DoesNotAdd()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            var initialCount = service.GetAllowedDomains().Count();

            // Act
            service.AddAllowedDomain("");
            service.AddAllowedDomain("   ");
            service.AddAllowedDomain(null!);

            // Assert
            Assert.Equal(initialCount, service.GetAllowedDomains().Count());
        }

        [Fact]
        public void RemoveAllowedDomain_WithExistingDomain_RemovesDomain()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            service.AddAllowedDomain("removeme.com");

            // Act
            service.RemoveAllowedDomain("removeme.com");

            // Assert
            Assert.False(service.IsAllowedDomain("removeme.com"));
        }

        [Fact]
        public void RemoveAllowedDomain_WithNonExistentDomain_DoesNotThrow()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);

            // Act & Assert (should not throw)
            service.RemoveAllowedDomain("nonexistent.com");
        }

        [Fact]
        public void GetAllowedDomains_ReturnsAllDomains()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            var testDomains = new[] { "domain1.com", "domain2.com", "*.domain3.com" };

            foreach (var domain in testDomains)
            {
                service.AddAllowedDomain(domain);
            }

            // Act
            var result = service.GetAllowedDomains().ToList();

            // Assert
            Assert.Contains("domain1.com", result);
            Assert.Contains("domain2.com", result);
            Assert.Contains("*.domain3.com", result);
        }

        [Fact]
        public async Task LoadConfigurationAsync_WithCorruptedFile_HandlesGracefully()
        {
            // Arrange
            var corruptedFile = Path.GetTempFileName();
            File.WriteAllText(corruptedFile, "{ invalid json content");
            _tempFiles.Add(corruptedFile);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:AllowedDomainsFile", corruptedFile }
                })
                .Build();

            var service = new DnsConfigurationService(_mockLogger.Object, config);

            // Act & Assert (should not throw)
            await service.LoadConfigurationAsync();

            // Verify error was logged
            TestHelpers.VerifyLoggerCalledWithLevel(_mockLogger, LogLevel.Error);
        }

        [Fact]
        public void DomainMatching_WithComplexScenarios_WorksCorrectly()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockLogger.Object, _configuration);
            
            // Add various domain patterns
            service.AddAllowedDomain("exact.com");
            service.AddAllowedDomain("*.wildcard.com");
            service.AddAllowedDomain("*.deep.nested.domain.com");

            // Act & Assert - Exact matches
            Assert.True(service.IsAllowedDomain("exact.com"));
            Assert.False(service.IsAllowedDomain("sub.exact.com"));

            // Act & Assert - Wildcard matches
            Assert.True(service.IsAllowedDomain("sub.wildcard.com"));
            Assert.True(service.IsAllowedDomain("another.wildcard.com"));
            Assert.False(service.IsAllowedDomain("wildcard.com"));

            // Act & Assert - Deep nested wildcards
            Assert.True(service.IsAllowedDomain("anything.deep.nested.domain.com"));
            Assert.False(service.IsAllowedDomain("deep.nested.domain.com"));
        }
    }
}