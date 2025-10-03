namespace PrivateDNS.Services
{
    public interface IDnsForwarder
    {
        Task<byte[]> ForwardDnsQueryAsync(byte[] query, CancellationToken cancellationToken = default);
    }
}