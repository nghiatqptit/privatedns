using Moq;
using PrivateDNS.Models;
using PrivateDNS.Services;
using PrivateDNS.Tests.Helpers;
using System.Diagnostics;

namespace PrivateDNS.Tests.Performance
{
    public class PerformanceTests : IDisposable
    {
        private readonly Mock<ILogger<DnsConfigurationService>> _mockConfigLogger;
        private readonly Mock<ILogger<DnsForwarder>> _mockForwarderLogger;
        private readonly IConfiguration _configuration;
        private readonly List<string> _tempFiles;

        public PerformanceTests()
        {
            _mockConfigLogger = TestHelpers.CreateMockLogger<DnsConfigurationService>();
            _mockForwarderLogger = TestHelpers.CreateMockLogger<DnsForwarder>();
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
        public void DnsMessage_Parsing_Performance()
        {
            // Arrange
            var testQueries = new List<byte[]>();
            for (int i = 0; i < 1000; i++)
            {
                testQueries.Add(TestHelpers.CreateMockDnsQuery($"test{i}.com", (ushort)i));
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 1000; i++)
            {
                var message = DnsMessage.FromBytes(testQueries[i]);
                Assert.NotNull(message);
            }

            stopwatch.Stop();

            // Assert - Should parse 1000 messages in reasonable time (less than 1 second)
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"DNS message parsing took {stopwatch.ElapsedMilliseconds}ms for 1000 messages");

            // Log performance metrics
            var avgTimePerMessage = stopwatch.ElapsedMilliseconds / 1000.0;
            Assert.True(avgTimePerMessage < 1.0, 
                $"Average time per message: {avgTimePerMessage}ms (should be < 1ms)");
        }

        [Fact]
        public void DnsMessage_Serialization_Performance()
        {
            // Arrange
            var messages = new List<DnsMessage>();
            for (int i = 0; i < 1000; i++)
            {
                messages.Add(new DnsMessage
                {
                    TransactionId = (ushort)i,
                    Flags = 0x0100,
                    Questions = new List<DnsQuestion>
                    {
                        new DnsQuestion { Name = $"test{i}.com", Type = 1, Class = 1 }
                    }
                });
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 1000; i++)
            {
                var bytes = messages[i].ToBytes();
                Assert.NotNull(bytes);
                Assert.NotEmpty(bytes);
            }

            stopwatch.Stop();

            // Assert - Should serialize 1000 messages in reasonable time
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"DNS message serialization took {stopwatch.ElapsedMilliseconds}ms for 1000 messages");
        }

        [Fact]
        public void DomainMatching_Performance_WithLargeDomainList()
        {
            // Arrange
            var largeDomainList = new List<string>();
            for (int i = 0; i < 10000; i++)
            {
                largeDomainList.Add($"domain{i}.com");
                if (i % 3 == 0) largeDomainList.Add($"*.subdomain{i}.com");
            }

            var tempFile = TestHelpers.CreateTempConfigFile(largeDomainList.ToArray());
            _tempFiles.Add(tempFile);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:AllowedDomainsFile", tempFile }
                })
                .Build();

            var service = new DnsConfigurationService(_mockConfigLogger.Object, config);

            // Load the large domain list
            service.LoadConfigurationAsync().Wait();

            var testDomains = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                testDomains.Add($"domain{i}.com"); // These should be allowed
                testDomains.Add($"test.subdomain{i * 3}.com"); // These should be allowed via wildcard
                testDomains.Add($"blocked{i}.com"); // These should be blocked
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            var results = new List<bool>();
            foreach (var domain in testDomains)
            {
                results.Add(service.IsAllowedDomain(domain));
            }

            stopwatch.Stop();

            // Assert
            Assert.Equal(3000, results.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Domain matching took {stopwatch.ElapsedMilliseconds}ms for 3000 domains against 10000 rules");

            var avgTimePerCheck = stopwatch.ElapsedMilliseconds / 3000.0;
            Assert.True(avgTimePerCheck < 0.5, 
                $"Average time per domain check: {avgTimePerCheck}ms (should be < 0.5ms)");
        }

        [Fact]
        public void WildcardMatching_Performance()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockConfigLogger.Object, _configuration);
            
            // Add various wildcard patterns
            for (int i = 0; i < 100; i++)
            {
                service.AddAllowedDomain($"*.pattern{i}.com");
                service.AddAllowedDomain($"*.deep.nested.pattern{i}.org");
            }

            var testDomains = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                testDomains.Add($"sub.pattern{i % 100}.com");
                testDomains.Add($"any.deep.nested.pattern{i % 100}.org");
                testDomains.Add($"blocked{i}.net");
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            var results = new List<bool>();
            foreach (var domain in testDomains)
            {
                results.Add(service.IsAllowedDomain(domain));
            }

            stopwatch.Stop();

            // Assert
            Assert.Equal(3000, results.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 500, 
                $"Wildcard matching took {stopwatch.ElapsedMilliseconds}ms for 3000 domains");

            // Verify some results are correct
            Assert.Contains(true, results); // Some should match wildcards
            Assert.Contains(false, results); // Some should be blocked
        }

        [Fact]
        public async Task ConfigurationLoading_Performance()
        {
            // Arrange
            var largeDomainList = new string[50000]; // 50K domains
            for (int i = 0; i < largeDomainList.Length; i++)
            {
                largeDomainList[i] = $"domain{i}.example{i % 100}.com";
            }

            var tempFile = TestHelpers.CreateTempConfigFile(largeDomainList);
            _tempFiles.Add(tempFile);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DnsProxy:AllowedDomainsFile", tempFile }
                })
                .Build();

            var service = new DnsConfigurationService(_mockConfigLogger.Object, config);

            var stopwatch = Stopwatch.StartNew();

            // Act
            await service.LoadConfigurationAsync();

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Loading 50K domains took {stopwatch.ElapsedMilliseconds}ms (should be < 5 seconds)");

            // Verify domains were loaded
            Assert.True(service.IsAllowedDomain("domain100.example0.com"));
            Assert.Equal(50000, service.GetAllowedDomains().Count());
        }

        [Fact]
        public void ConcurrentDomainChecking_Performance()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockConfigLogger.Object, _configuration);
            
            // Add test domains
            for (int i = 0; i < 1000; i++)
            {
                service.AddAllowedDomain($"concurrent{i}.com");
            }

            var testDomains = new List<string>();
            for (int i = 0; i < 10000; i++)
            {
                testDomains.Add($"concurrent{i % 1000}.com");
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Test concurrent access
            var tasks = new List<Task<bool[]>>();
            var batchSize = testDomains.Count / Environment.ProcessorCount;

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var start = i * batchSize;
                var end = Math.Min(start + batchSize, testDomains.Count);
                var batch = testDomains.Skip(start).Take(end - start).ToList();

                tasks.Add(Task.Run(() => 
                    batch.Select(domain => service.IsAllowedDomain(domain)).ToArray()));
            }

            var results = Task.WhenAll(tasks).Result;
            stopwatch.Stop();

            // Assert
            var totalResults = results.SelectMany(r => r).Count();
            Assert.Equal(testDomains.Count, totalResults);
            
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
                $"Concurrent domain checking took {stopwatch.ElapsedMilliseconds}ms for {testDomains.Count} domains");
        }

        [Fact]
        public void MemoryUsage_DomainStorage()
        {
            // Arrange
            var service = new DnsConfigurationService(_mockConfigLogger.Object, _configuration);
            
            // Measure initial memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Add many domains
            for (int i = 0; i < 10000; i++)
            {
                service.AddAllowedDomain($"memory-test-domain-{i}.com");
                if (i % 3 == 0)
                {
                    service.AddAllowedDomain($"*.wildcard-memory-test-{i}.com");
                }
            }

            // Measure final memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            // Assert - Memory usage should be reasonable (less than 10MB for 10K domains)
            Assert.True(memoryUsed < 10 * 1024 * 1024, 
                $"Memory usage: {memoryUsed / 1024.0 / 1024.0:F2} MB for 10K domains (should be < 10MB)");

            // Verify functionality still works
            Assert.True(service.IsAllowedDomain("memory-test-domain-100.com"));
            Assert.True(service.IsAllowedDomain("sub.wildcard-memory-test-300.com"));
        }
    }
}