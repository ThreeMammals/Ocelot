namespace Ocelot.Configuration.File
{
    public class FileServiceDiscoveryProvider
    {
        public string Host {get;set;}
        public int Port { get; set; }
        public string Type { get; set; }
        public string Token { get; set; }
        public string ConfigurationKey { get; set; }
    }
}
