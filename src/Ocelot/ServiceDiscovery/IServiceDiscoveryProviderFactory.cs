using System;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceDiscoveryProviderFactory
    {
        IServiceDiscoveryProvider Get(ServiceProviderConfiguraion serviceConfig);
    }
}