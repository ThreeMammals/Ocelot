namespace Ocelot.ServiceDiscovery
{
    public class ServiceProviderConfiguraion
    {
        public ServiceProviderConfiguraion(string serviceName, string downstreamHost, 
        int downstreamPort, bool useServiceDiscovery, string serviceDiscoveryProvider)
        {
            ServiceName = serviceName;
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
            UseServiceDiscovery = useServiceDiscovery;
            ServiceDiscoveryProvider = serviceDiscoveryProvider;
        }

        public string ServiceName { get; }
        public string DownstreamHost { get; }
        public int DownstreamPort { get; }
        public bool UseServiceDiscovery { get; }
        public string ServiceDiscoveryProvider {get;}
    }
}