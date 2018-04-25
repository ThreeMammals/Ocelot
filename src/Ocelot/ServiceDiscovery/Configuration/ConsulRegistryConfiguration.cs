namespace Ocelot.ServiceDiscovery.Configuration
{
    public class ConsulRegistryConfiguration
    {
        public ConsulRegistryConfiguration(string host, int port, string keyOfServiceInConsul, string token)
        {
            Host = host;
            Port = port;
            KeyOfServiceInConsul = keyOfServiceInConsul;
            Token = token;
        }

        public string KeyOfServiceInConsul { get; }
        public string Host { get; }
        public int Port { get; }
        public string Token { get; }
    }
}
