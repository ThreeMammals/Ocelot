using Ocelot.Configuration;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceDiscoveryProviderFactory
    {
        Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route);
    }
}
