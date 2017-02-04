using System;
using Ocelot.Configuration;

namespace Ocelot.ServiceDiscovery
{
    public interface IServiceDiscoveryProviderFactory
    {
        IServiceDiscoveryProvider Get(ServiceProviderConfiguraion serviceConfig);
    }
}