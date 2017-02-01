using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceProviderFactory : IServiceProviderFactory
    {
        public  IServiceProvider Get(ServiceConfiguraion serviceConfig)
        {
            var services = new List<Service>()
            {
                new Service(serviceConfig.ServiceName, new HostAndPort(serviceConfig.DownstreamHost, serviceConfig.DownstreamPort))
            };

            return new ConfigurationServiceProvider(services);
        }
    }

    public class ServiceConfiguraion
    {
        public ServiceConfiguraion(string serviceName, string downstreamHost, int downstreamPort, bool useServiceDiscovery)
        {
            ServiceName = serviceName;
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
            UseServiceDiscovery = useServiceDiscovery;
        }

        public string ServiceName { get; }
        public string DownstreamHost { get; }
        public int DownstreamPort { get; }
        public bool UseServiceDiscovery { get; }
    }
}