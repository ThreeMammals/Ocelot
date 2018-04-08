using Consul;
using Ocelot.ServiceDiscovery.Configuration;

namespace Ocelot.Infrastructure.Consul
{
    public interface IConsulClientFactory
    {
        ConsulClient Get(ConsulRegistryConfiguration config);
    }
}
