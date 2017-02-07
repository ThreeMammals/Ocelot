namespace Ocelot.ServiceDiscovery
{
    public class ConsulRegistryConfiguration
    {
        public ConsulRegistryConfiguration(string hostName, int port, string serviceName)
        {
            HostName = hostName;
            Port = port;
            ServiceName = serviceName;
        }

        public string ServiceName { get; private set; }
        public string HostName { get; private set; }
        public int Port { get; private set; }
    }
}