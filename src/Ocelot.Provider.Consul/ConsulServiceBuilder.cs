using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Consul;

public class ConsulServiceBuilder : IConsulServiceBuilder
{
    private readonly ConsulRegistryConfiguration _configuration;
    private readonly IConsulClient _client;
    private readonly IOcelotLogger _logger;

    public ConsulServiceBuilder(
        Func<ConsulRegistryConfiguration> configurationFactory,
        IConsulClientFactory clientFactory,
        IOcelotLoggerFactory loggerFactory)
    {
        _configuration = configurationFactory.Invoke();
        _client = clientFactory.Get(_configuration);
        _logger = loggerFactory.CreateLogger<ConsulServiceBuilder>();
    }

    public virtual Service BuildService(ServiceEntry entry, IEnumerable<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(entry);
        nodes ??= _client.Catalog.Nodes().Result?.Response;
        return BuildServiceInternal(entry, nodes);
    }

    public virtual async Task<Service> BuildServiceAsync(ServiceEntry entry, IEnumerable<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(entry);
        nodes ??= (await _client.Catalog.Nodes())?.Response;
        return BuildServiceInternal(entry, nodes);
    }

    protected virtual Service BuildServiceInternal(ServiceEntry entry, IEnumerable<Node> nodes)
    {
        var serviceNode = nodes?.FirstOrDefault(n => n.Address == entry.Service.Address);
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
