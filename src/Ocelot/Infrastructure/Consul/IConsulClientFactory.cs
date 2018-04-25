using Consul;
using Ocelot.ServiceDiscovery.Configuration;

namespace Ocelot.Infrastructure.Consul
{
    public interface IConsulClientFactory
    {
        IConsulClient Get(ConsulRegistryConfiguration config);
    }
}
