namespace Ocelot.Provider.Nacos
{
    public class NacosProviderOptions
    {
        public string ServerAddresses { get; set; } = "http://localhost:8848";
        public string Namespace { get; set; } = "public";
        public int ListenInterval { get; set; } = 1000;
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
