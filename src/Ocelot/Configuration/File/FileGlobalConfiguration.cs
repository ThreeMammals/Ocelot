
namespace Ocelot.Configuration.File
{
    public class FileGlobalConfiguration
    {
        public FileGlobalConfiguration()
        {
            ServiceDiscoveryProvider = new FileServiceDiscoveryProvider();
            RateLimitOptions = new FileRateLimitOptions();
        }

        public string RequestIdKey { get; set; }

        public FileServiceDiscoveryProvider ServiceDiscoveryProvider {get;set;}

        public FileRateLimitOptions RateLimitOptions { get; set; }
    }
}
