namespace Ocelot.Configuration
{
    public class ServiceProviderConfiguration
    {
        public ServiceProviderConfiguration(string serviceDiscoveryProvider, string serviceProviderHost, int serviceProviderPort)
        {
            ServiceDiscoveryProvider = serviceDiscoveryProvider;
            ServiceProviderHost = serviceProviderHost;
            ServiceProviderPort = serviceProviderPort;
        }

        public string ServiceDiscoveryProvider { get; }
        public string ServiceProviderHost { get; private set; }
        public int ServiceProviderPort { get; private set; }
    }
}