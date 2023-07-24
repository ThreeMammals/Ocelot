using Ocelot.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using System;

namespace Ocelot.ServiceDiscovery
{
    public delegate IServiceDiscoveryProvider ServiceDiscoveryFinderDelegate(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route);
}
