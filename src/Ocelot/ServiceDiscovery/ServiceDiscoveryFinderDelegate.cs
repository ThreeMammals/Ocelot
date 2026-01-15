using Ocelot.Configuration;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.ServiceDiscovery;

public delegate IServiceDiscoveryProvider ServiceDiscoveryFinderDelegate(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route);
