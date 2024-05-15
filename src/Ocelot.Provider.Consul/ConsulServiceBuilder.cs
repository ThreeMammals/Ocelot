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

    public ConsulRegistryConfiguration Configuration => _configuration;

    public virtual bool IsValid(ServiceEntry entry)
    {
        var address = entry.Service.Address;
        return !string.IsNullOrEmpty(address)
            && !address.Contains($"{Uri.UriSchemeHttp}://")
            && !address.Contains($"{Uri.UriSchemeHttps}://")
            && entry.Service.Port > 0;
    }

    public virtual IEnumerable<Service> BuildServices(ServiceEntry[] entries, Node[] nodes)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var services = new List<Service>();

        foreach (var serviceEntry in entries)
        {
            var service = serviceEntry.Service;
            if (IsValid(serviceEntry))
            {
                var serviceNode = nodes?.FirstOrDefault(n => n.Address == service.Address);
                var item = CreateService(serviceEntry, serviceNode);
                if (item != null)
                {
                    services.Add(item);
                }
            }
            else
            {
                _logger.LogWarning(
                    () => $"Unable to use service address: '{service.Address}' and port: {service.Port} as it is invalid for the service: '{service.Service}'. Address must contain host only e.g. 'localhost', and port must be greater than 0.");
            }
        }

        return services;
    }

    public virtual Service CreateService(ServiceEntry entry, Node node)
        => new(
            GetServiceName(entry, node),
            GetServiceHostAndPort(entry, node),
            GetServiceId(entry, node),
            GetServiceVersion(entry, node),
            GetServiceTags(entry, node)
        );

    protected virtual string GetServiceName(ServiceEntry entry, Node node)
        => entry.Service.Service;

    protected virtual ServiceHostAndPort GetServiceHostAndPort(ServiceEntry entry, Node node)
        => new(
            downstreamHost: node != null ? node.Name : entry.Service.Address,
            downstreamPort: entry.Service.Port);

    protected virtual string GetServiceId(ServiceEntry entry, Node serviceNode)
        => entry.Service.ID;

    protected virtual string GetServiceVersion(ServiceEntry entry, Node serviceNode)
        => entry.Service.Tags?
            .FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
            .TrimStart(VersionPrefix);

    protected virtual IEnumerable<string> GetServiceTags(ServiceEntry entry, Node serviceNode)
        => entry.Service.Tags ?? Enumerable.Empty<string>();

    private const string VersionPrefix = "version-";
}
