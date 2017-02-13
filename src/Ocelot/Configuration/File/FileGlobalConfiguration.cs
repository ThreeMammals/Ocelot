namespace Ocelot.Configuration.File
{
    public class FileGlobalConfiguration
    {
        public FileGlobalConfiguration()
        {
            ServiceDiscoveryProvider = new FileServiceDiscoveryProvider();
        }
        public string RequestIdKey { get; set; }
        public FileServiceDiscoveryProvider ServiceDiscoveryProvider {get;set;}
        public string AdministrationPath {get;set;}
    }
}
