using Ocelot.Values;

namespace Ocelot.Provider.Consul.Interfaces;

public interface IConsulServiceBuilder
{
    Task<Service> BuildServiceAsync(IConsulClient client, ConsulRegistryConfiguration configuration, ServiceEntry entry);
}
