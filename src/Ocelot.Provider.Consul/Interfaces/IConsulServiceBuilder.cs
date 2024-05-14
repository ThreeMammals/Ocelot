using Ocelot.Values;

namespace Ocelot.Provider.Consul.Interfaces;

public interface IConsulServiceBuilder
{
    Service BuildService(ServiceEntry entry, IEnumerable<Node> nodes);
    Task<Service> BuildServiceAsync(ServiceEntry entry, IEnumerable<Node> nodes);
}
