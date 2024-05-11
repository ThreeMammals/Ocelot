using Ocelot.Infrastructure.Extensions;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Consul;

public class ConsulServiceBuilder : IConsulServiceBuilder
{
    public async Task<Service> BuildServiceAsync(IConsulClient client, ConsulRegistryConfiguration configuration, ServiceEntry entry)
    {
        var nodes = await client.Catalog.Nodes();
        Node serviceNode = nodes?.Response?.FirstOrDefault(n => n.Address == entry.Service.Address);
        return CreateService(entry, serviceNode);
    }

    private static Service CreateService(ServiceEntry serviceEntry, Node serviceNode)
    {
        var service = serviceEntry.Service;
        return new Service(
            service.Service,
            new ServiceHostAndPort(
                serviceNode == null ? service.Address : serviceNode.Name,
                service.Port),
            service.ID,
            GetVersionFromStrings(service.Tags),
            service.Tags ?? Enumerable.Empty<string>());
    }

    private const string VersionPrefix = "version-";

    private static string GetVersionFromStrings(IEnumerable<string> strings)
        => strings?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
            .TrimStart(VersionPrefix);
}
