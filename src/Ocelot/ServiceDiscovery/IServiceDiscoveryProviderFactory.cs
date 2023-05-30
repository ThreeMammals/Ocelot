using Ocelot.Configuration;

using Ocelot.ServiceDiscovery.Providers;

using Ocelot.Responses;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceDiscoveryProviderFactory
    {
        Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route);
    }
}
