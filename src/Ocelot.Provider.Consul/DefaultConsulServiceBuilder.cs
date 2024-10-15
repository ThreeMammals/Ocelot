using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Consul;

public class DefaultConsulServiceBuilder : IConsulServiceBuilder
{
    private readonly HttpContext _context;
    private readonly IConsulClientFactory _clientFactory;
    private readonly IOcelotLoggerFactory _loggerFactory;

    private ConsulRegistryConfiguration _configuration;
    private IConsulClient _client;
    private IOcelotLogger _logger;

    public DefaultConsulServiceBuilder(
        IHttpContextAccessor contextAccessor,
        IConsulClientFactory clientFactory,
        IOcelotLoggerFactory loggerFactory)
    {
        _context = contextAccessor.HttpContext;
        _clientFactory = clientFactory;
        _loggerFactory = loggerFactory;
    }

    // TODO See comment in the interface about the privacy. The goal is to eliminate IBC!
    // So, we need more abstract type, and ServiceProviderConfiguration is a good choice. The rest of props can be obtained from HttpContext
    protected /*public*/ ConsulRegistryConfiguration Configuration => _configuration
        ??= _context.Items.TryGetValue(nameof(ConsulRegistryConfiguration), out var value)
            ? value as ConsulRegistryConfiguration : default;
    protected IConsulClient Client => _client ??= _clientFactory.Get(Configuration);
    protected IOcelotLogger Logger => _logger ??= _loggerFactory.CreateLogger<DefaultConsulServiceBuilder>();

    public virtual bool IsValid(ServiceEntry entry)
    {
        var service = entry.Service;
        var address = service.Address;
        bool valid = !string.IsNullOrEmpty(address)
            && !address.StartsWith(Uri.UriSchemeHttp + "://", StringComparison.OrdinalIgnoreCase)
            && !address.StartsWith(Uri.UriSchemeHttps + "://", StringComparison.OrdinalIgnoreCase)
            && service.Port > 0;

        if (!valid)
        {
            Logger.LogWarning(
                () => $"Unable to use service address: '{service.Address}' and port: {service.Port} as it is invalid for the service: '{service.Service}'. Address must contain host only e.g. 'localhost', and port must be greater than 0.");
        }

        return valid;
    }

    public virtual IEnumerable<Service> BuildServices(ServiceEntry[] entries, Node[] nodes)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var services = new List<Service>(entries.Length);

        foreach (var serviceEntry in entries)
        {
            if (IsValid(serviceEntry))
            {
                var serviceNode = GetNode(serviceEntry, nodes);
                var item = CreateService(serviceEntry, serviceNode);
                if (item != null)
                {
                    services.Add(item);
                }
            }
        }

        return services;
    }

    protected virtual Node GetNode(ServiceEntry entry, Node[] nodes)
        => entry?.Node ?? nodes?.FirstOrDefault(n => n.Address == entry?.Service?.Address);

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
            GetDownstreamHost(entry, node),
            entry.Service.Port);

    protected virtual string GetDownstreamHost(ServiceEntry entry, Node node)
        => node != null ? node.Name : entry.Service.Address;

    protected virtual string GetServiceId(ServiceEntry entry, Node node)
        => entry.Service.ID;

    protected virtual string GetServiceVersion(ServiceEntry entry, Node node)
        => entry.Service.Tags
            ?.FirstOrDefault(tag => tag.StartsWith(VersionPrefix, StringComparison.Ordinal))
            ?.TrimStart(VersionPrefix)
            ?? string.Empty;

    protected virtual IEnumerable<string> GetServiceTags(ServiceEntry entry, Node node)
        => entry.Service.Tags ?? Enumerable.Empty<string>();

    private const string VersionPrefix = "version-";
}
