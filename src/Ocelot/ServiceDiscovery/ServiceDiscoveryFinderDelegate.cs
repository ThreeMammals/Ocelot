namespace Ocelot.ServiceDiscovery
{
    using System;
    using Ocelot.Configuration;
    using Providers;

    public delegate IServiceDiscoveryProvider ServiceDiscoveryFinderDelegate(IServiceProvider provider, ServiceProviderConfiguration config, string key);
}
