namespace Ocelot.ServiceDiscovery
{
    public class ConsulRegistryConfiguration
    {
        public ConsulRegistryConfiguration(string hostName, int port, string keyOfServiceInConsul)
        {
            HostName = hostName;
            Port = port;
            KeyOfServiceInConsul = keyOfServiceInConsul;
        }

        public string KeyOfServiceInConsul { get; private set; }
        public string HostName { get; private set; }
        public int Port { get; private set; }
    }
}