using Moq;

namespace PrivateDNS.Tests.Helpers
{
    public static class TestHelpers
    {
        public static IConfiguration CreateTestConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("TestData/test-appsettings.json", optional: false)
                .Build();

            return configuration;
        }

        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        public static void VerifyLoggerCalled<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel, string expectedMessage)
        {
            mockLogger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        public static void VerifyLoggerCalledWithLevel<T>(Mock<ILogger<T>> mockLogger, LogLevel logLevel)
        {
            mockLogger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        public static byte[] CreateMockDnsQuery(string domain, ushort transactionId = 1234)
        {
            // Create a simple DNS query packet for testing
            var query = new List<byte>();
            
            // Header (12 bytes)
            query.AddRange(BitConverter.GetBytes((ushort)transactionId).Reverse()); // Transaction ID
            query.AddRange(new byte[] { 0x01, 0x00 }); // Flags: Standard query
            query.AddRange(new byte[] { 0x00, 0x01 }); // Questions: 1
            query.AddRange(new byte[] { 0x00, 0x00 }); // Answer RRs: 0
            query.AddRange(new byte[] { 0x00, 0x00 }); // Authority RRs: 0
            query.AddRange(new byte[] { 0x00, 0x00 }); // Additional RRs: 0
            
            // Question section
            var domainParts = domain.Split('.');
            foreach (var part in domainParts)
            {
                query.Add((byte)part.Length);
                query.AddRange(System.Text.Encoding.ASCII.GetBytes(part));
            }
            query.Add(0x00); // End of domain name
            
            query.AddRange(new byte[] { 0x00, 0x01 }); // Type: A
            query.AddRange(new byte[] { 0x00, 0x01 }); // Class: IN
            
            return query.ToArray();
        }

        public static string CreateTempConfigFile(string[] domains)
        {
            var tempFile = Path.GetTempFileName();
            var json = System.Text.Json.JsonSerializer.Serialize(domains, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(tempFile, json);
            return tempFile;
        }

        public static void CleanupTempFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}