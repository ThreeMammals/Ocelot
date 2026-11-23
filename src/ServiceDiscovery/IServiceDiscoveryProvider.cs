using Ocelot.Values;

namespace Ocelot.ServiceDiscovery;

public interface IServiceDiscoveryProvider
{
    string Name { get; }

    Task<List<Service>> GetAsync();
}
