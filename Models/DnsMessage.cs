using System.Net;

namespace PrivateDNS.Models
{
    public class DnsMessage
    {
        public ushort TransactionId { get; set; }
        public ushort Flags { get; set; }
        public ushort QuestionCount { get; set; }
        public ushort AnswerCount { get; set; }
        public ushort AuthorityCount { get; set; }
        public ushort AdditionalCount { get; set; }
        public List<DnsQuestion> Questions { get; set; } = new();
        public List<DnsAnswer> Answers { get; set; } = new();

        public byte[] ToBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Header
            writer.Write(IPAddress.HostToNetworkOrder((short)TransactionId));
            writer.Write(IPAddress.HostToNetworkOrder((short)Flags));
            writer.Write(IPAddress.HostToNetworkOrder((short)Questions.Count));
            writer.Write(IPAddress.HostToNetworkOrder((short)Answers.Count));
            writer.Write(IPAddress.HostToNetworkOrder((short)AuthorityCount));
            writer.Write(IPAddress.HostToNetworkOrder((short)AdditionalCount));

            // Questions
            foreach (var question in Questions)
            {
                WriteDomainName(writer, question.Name);
                writer.Write(IPAddress.HostToNetworkOrder((short)question.Type));
                writer.Write(IPAddress.HostToNetworkOrder((short)question.Class));
            }

            // Answers
            foreach (var answer in Answers)
            {
                WriteDomainName(writer, answer.Name);
                writer.Write(IPAddress.HostToNetworkOrder((short)answer.Type));
                writer.Write(IPAddress.HostToNetworkOrder((short)answer.Class));
                writer.Write(IPAddress.HostToNetworkOrder((int)answer.Ttl));
                writer.Write(IPAddress.HostToNetworkOrder((short)answer.Data.Length));
                writer.Write(answer.Data);
            }

            return stream.ToArray();
        }

        public static DnsMessage FromBytes(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            var message = new DnsMessage
            {
                TransactionId = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16()),
                Flags = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16()),
                QuestionCount = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16()),
                AnswerCount = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16()),
                AuthorityCount = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16()),
                AdditionalCount = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16())
            };

            // Read questions
            for (int i = 0; i < message.QuestionCount; i++)
            {
                var question = new DnsQuestion
                {
                    Name = ReadDomainName(reader),
                    Type = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16()),
                    Class = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16())
                };
                message.Questions.Add(question);
            }

            return message;
        }

        private static void WriteDomainName(BinaryWriter writer, string domain)
        {
            var parts = domain.Split('.');
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                writer.Write((byte)part.Length);
                writer.Write(System.Text.Encoding.ASCII.GetBytes(part));
            }
            writer.Write((byte)0);
        }

        private static string ReadDomainName(BinaryReader reader)
        {
            var parts = new List<string>();
            byte length;
            while ((length = reader.ReadByte()) != 0)
            {
                if ((length & 0xC0) == 0xC0)
                {
                    // Pointer - skip for simplicity
                    reader.ReadByte();
                    break;
                }
                var part = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(length));
                parts.Add(part);
            }
            return string.Join(".", parts);
        }
    }

    public class DnsQuestion
    {
        public string Name { get; set; } = string.Empty;
        public ushort Type { get; set; }
        public ushort Class { get; set; }
    }

    public class DnsAnswer
    {
        public string Name { get; set; } = string.Empty;
        public ushort Type { get; set; }
        public ushort Class { get; set; }
        public uint Ttl { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}