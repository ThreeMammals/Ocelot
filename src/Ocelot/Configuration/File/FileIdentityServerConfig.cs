namespace Ocelot.Configuration.File
{
    public class FileIdentityServerConfig
    {
        public string ProviderRootUrl { get; set; }
        public string ApiName { get; set; }
        public bool RequireHttps { get; set; }
        public string ApiSecret { get; set; }
    }
}