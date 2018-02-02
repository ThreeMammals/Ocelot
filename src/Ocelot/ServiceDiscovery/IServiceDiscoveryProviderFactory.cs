using Ocelot.Configuration;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceDiscoveryProviderFactory
    {
        IServiceDiscoveryProvider Get(ServiceProviderConfiguration serviceConfig, ReRoute reRoute);
    }
}