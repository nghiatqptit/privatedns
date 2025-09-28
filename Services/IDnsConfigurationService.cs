namespace PrivateDNS.Services
{
    public interface IDnsConfigurationService
    {
        bool IsAllowedDomain(string domain);
        Task LoadConfigurationAsync();
        void AddAllowedDomain(string domain);
        void RemoveAllowedDomain(string domain);
        IEnumerable<string> GetAllowedDomains();
    }
}