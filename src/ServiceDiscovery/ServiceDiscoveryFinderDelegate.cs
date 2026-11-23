using Ocelot.Configuration;
namespace Ocelot.ServiceDiscovery;

public delegate IServiceDiscoveryProvider ServiceDiscoveryFinderDelegate(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route);
