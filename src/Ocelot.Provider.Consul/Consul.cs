using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.Provider.Consul;

public class Consul : IServiceDiscoveryProvider
{
    private const string VersionPrefix = "version-";
    private readonly ConsulRegistryConfiguration _config;
    private readonly IConsulClient _consul;
    private readonly IOcelotLogger _logger;

    public Consul(ConsulRegistryConfiguration config, IOcelotLoggerFactory factory, IConsulClientFactory clientFactory)
    {
        _config = config;
        _consul = clientFactory.Get(_config);
        _logger = factory.CreateLogger<Consul>();
    }

    public async Task<List<Service>> GetAsync()
    {
        var queryResult = await _consul.Health.Service(_config.KeyOfServiceInConsul, string.Empty, true);

        var services = new List<Service>();

        foreach (var serviceEntry in queryResult.Response)
        {
            var service = serviceEntry.Service;
            if (IsValid(service))
            {
                var nodes = await _consul.Catalog.Nodes();
                if (nodes.Response == null)
                {
                    services.Add(BuildService(serviceEntry, null));
                }
                else
                {
                    var serviceNode = nodes.Response.FirstOrDefault(n => n.Address == service.Address);
                    services.Add(BuildService(serviceEntry, serviceNode));
                }
            }
            else
            {
                _logger.LogWarning(
                    () => $"Unable to use service address: '{service.Address}' and port: {service.Port} as it is invalid for the service: '{service.Service}'. Address must contain host only e.g. 'localhost', and port must be greater than 0.");
            }
        }

        return services.ToList();
    }

    private static Service BuildService(ServiceEntry serviceEntry, Node serviceNode)
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

    private static bool IsValid(AgentService service)
        => !string.IsNullOrEmpty(service.Address)
        && !service.Address.Contains($"{Uri.UriSchemeHttp}://")
        && !service.Address.Contains($"{Uri.UriSchemeHttps}://")
        && service.Port > 0;

    private static string GetVersionFromStrings(IEnumerable<string> strings)
        => strings?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
            .TrimStart(VersionPrefix);
}
