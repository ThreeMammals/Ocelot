namespace Ocelot.Configuration
{
    public class ServiceProviderConfiguration
    {
        public ServiceProviderConfiguration(string serviceName, string downstreamHost,
            int downstreamPort, bool useServiceDiscovery, string serviceDiscoveryProvider, string serviceProviderHost, int serviceProviderPort)
        {
            ServiceName = serviceName;
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
            UseServiceDiscovery = useServiceDiscovery;
            ServiceDiscoveryProvider = serviceDiscoveryProvider;
            ServiceProviderHost = serviceProviderHost;
            ServiceProviderPort = serviceProviderPort;
        }

        public string ServiceName { get; }
        public string DownstreamHost { get; }
        public int DownstreamPort { get; }
        public bool UseServiceDiscovery { get; }
        public string ServiceDiscoveryProvider { get; }
        public string ServiceProviderHost { get; private set; }
        public int ServiceProviderPort { get; private set; }
    }
}