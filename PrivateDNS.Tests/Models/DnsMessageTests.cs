using PrivateDNS.Models;
using PrivateDNS.Tests.Helpers;
using System.Net;

namespace PrivateDNS.Tests.Models
{
    public class DnsMessageTests
    {
        [Fact]
        public void DnsMessage_Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var message = new DnsMessage();

            // Assert
            Assert.Equal(0, message.TransactionId);
            Assert.Equal(0, message.Flags);
            Assert.Equal(0, message.QuestionCount);
            Assert.Equal(0, message.AnswerCount);
            Assert.Equal(0, message.AuthorityCount);
            Assert.Equal(0, message.AdditionalCount);
            Assert.NotNull(message.Questions);
            Assert.NotNull(message.Answers);
            Assert.Empty(message.Questions);
            Assert.Empty(message.Answers);
        }

        [Fact]
        public void DnsMessage_ToBytes_CreatesValidPacket()
        {
            // Arrange
            var message = new DnsMessage
            {
                TransactionId = 1234,
                Flags = 0x0100,
                Questions = new List<DnsQuestion>
                {
                    new DnsQuestion { Name = "google.com", Type = 1, Class = 1 }
                }
            };

            // Act
            var bytes = message.ToBytes();

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 12); // At least header size
            
            // Check transaction ID (first 2 bytes in network order)
            var transactionId = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, 0));
            Assert.Equal(1234, transactionId);
        }

        [Fact]
        public void DnsMessage_FromBytes_ParsesValidPacket()
        {
            // Arrange
            var testQuery = TestHelpers.CreateMockDnsQuery("test.com", 5678);

            // Act
            var message = DnsMessage.FromBytes(testQuery);

            // Assert
            Assert.NotNull(message);
            Assert.Equal(5678, message.TransactionId);
            Assert.Equal(1, message.QuestionCount);
            Assert.Single(message.Questions);
            Assert.Equal("test.com", message.Questions[0].Name);
        }

        [Fact]
        public void DnsMessage_FromBytes_WithEmptyData_ThrowsException()
        {
            // Arrange
            var emptyData = Array.Empty<byte>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => DnsMessage.FromBytes(emptyData));
        }

        [Fact]
        public void DnsMessage_FromBytes_WithInvalidData_ThrowsException()
        {
            // Arrange
            var invalidData = new byte[] { 0x01, 0x02, 0x03 }; // Too short

            // Act & Assert
            Assert.Throws<ArgumentException>(() => DnsMessage.FromBytes(invalidData));
        }

        [Theory]
        [InlineData("google.com")]
        [InlineData("sub.domain.example.org")]
        [InlineData("a.b.c.d.e.f")]
        public void DnsMessage_RoundTrip_PreservesData(string domain)
        {
            // Arrange
            var originalMessage = new DnsMessage
            {
                TransactionId = 9999,
                Flags = 0x0100,
                Questions = new List<DnsQuestion>
                {
                    new DnsQuestion { Name = domain, Type = 1, Class = 1 }
                }
            };

            // Act
            var bytes = originalMessage.ToBytes();
            var parsedMessage = DnsMessage.FromBytes(bytes);

            // Assert
            Assert.Equal(originalMessage.TransactionId, parsedMessage.TransactionId);
            Assert.Equal(1, parsedMessage.Questions.Count);
            Assert.Equal(domain, parsedMessage.Questions[0].Name);
            Assert.Equal(1, parsedMessage.Questions[0].Type);
            Assert.Equal(1, parsedMessage.Questions[0].Class);
        }

        [Fact]
        public void DnsMessage_WithAnswer_SerializesCorrectly()
        {
            // Arrange
            var message = new DnsMessage
            {
                TransactionId = 1111,
                Flags = 0x8180,
                Questions = new List<DnsQuestion>
                {
                    new DnsQuestion { Name = "example.com", Type = 1, Class = 1 }
                },
                Answers = new List<DnsAnswer>
                {
                    new DnsAnswer 
                    { 
                        Name = "example.com", 
                        Type = 1, 
                        Class = 1, 
                        Ttl = 300,
                        Data = IPAddress.Parse("192.168.1.1").GetAddressBytes()
                    }
                }
            };

            // Act
            var bytes = message.ToBytes();

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 12); // Header + question + answer
        }
    }

    public class DnsQuestionTests
    {
        [Fact]
        public void DnsQuestion_Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var question = new DnsQuestion();

            // Assert
            Assert.Equal(string.Empty, question.Name);
            Assert.Equal(0, question.Type);
            Assert.Equal(0, question.Class);
        }

        [Fact]
        public void DnsQuestion_Properties_CanBeSet()
        {
            // Arrange
            var question = new DnsQuestion();

            // Act
            question.Name = "test.com";
            question.Type = 1;
            question.Class = 1;

            // Assert
            Assert.Equal("test.com", question.Name);
            Assert.Equal(1, question.Type);
            Assert.Equal(1, question.Class);
        }
    }

    public class DnsAnswerTests
    {
        [Fact]
        public void DnsAnswer_Constructor_SetsDefaultValues()
        {
            // Arrange & Act
            var answer = new DnsAnswer();

            // Assert
            Assert.Equal(string.Empty, answer.Name);
            Assert.Equal(0, answer.Type);
            Assert.Equal(0, answer.Class);
            Assert.Equal(0u, answer.Ttl);
            Assert.NotNull(answer.Data);
            Assert.Empty(answer.Data);
        }

        [Fact]
        public void DnsAnswer_Properties_CanBeSet()
        {
            // Arrange
            var answer = new DnsAnswer();
            var testData = new byte[] { 192, 168, 1, 1 };

            // Act
            answer.Name = "test.com";
            answer.Type = 1;
            answer.Class = 1;
            answer.Ttl = 300;
            answer.Data = testData;

            // Assert
            Assert.Equal("test.com", answer.Name);
            Assert.Equal(1, answer.Type);
            Assert.Equal(1, answer.Class);
            Assert.Equal(300u, answer.Ttl);
            Assert.Equal(testData, answer.Data);
        }
    }
}