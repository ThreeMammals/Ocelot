using Ocelot.Values;

namespace Ocelot.Provider.Consul.Interfaces;

public interface IConsulServiceBuilder
{
    ConsulRegistryConfiguration Configuration { get; }
    bool IsValid(ServiceEntry entry);
    IEnumerable<Service> BuildServices(ServiceEntry[] entries, Node[] nodes);
    Service CreateService(ServiceEntry serviceEntry, Node serviceNode);
}
