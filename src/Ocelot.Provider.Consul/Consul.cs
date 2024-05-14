using Ocelot.Logging;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.Provider.Consul;

public class Consul : IServiceDiscoveryProvider
{
    private readonly ConsulRegistryConfiguration _config;
    private readonly IConsulClient _consul;
    private readonly IOcelotLogger _logger;
    private readonly IConsulServiceBuilder _serviceBuilder;

    public Consul(
        ConsulRegistryConfiguration config,
        IOcelotLoggerFactory factory,
        IConsulClientFactory clientFactory,
        IConsulServiceBuilder serviceBuilder)
    {
        _config = config;
        _consul = clientFactory.Get(_config);
        _logger = factory.CreateLogger<Consul>();
        _serviceBuilder = serviceBuilder;
    }

    public virtual async Task<List<Service>> GetAsync()
    {
        var services = new List<Service>();
        var entriesTask = _consul.Health.Service(_config.KeyOfServiceInConsul, string.Empty, true);
        var nodesTask = _consul.Catalog.Nodes();

        await Task.WhenAll(entriesTask, nodesTask);

        var entries = entriesTask.Result.Response;
        var nodes = nodesTask.Result.Response;

        foreach (var serviceEntry in entries)
        {
            if (IsValid(serviceEntry))
            {
                var item = _serviceBuilder.BuildService(serviceEntry, nodes);
                if (item != null)
                {
                    services.Add(item);
                }
            }
            else
            {
                var service = serviceEntry.Service;
                _logger.LogWarning(
                    () => $"Unable to use service address: '{service.Address}' and port: {service.Port} as it is invalid for the service: '{service.Service}'. Address must contain host only e.g. 'localhost', and port must be greater than 0.");
            }
        }

        return services;
    }

    protected virtual bool IsValid(ServiceEntry entry)
    {
        var address = entry.Service.Address;
        return !string.IsNullOrEmpty(address)
            && !address.Contains($"{Uri.UriSchemeHttp}://")
            && !address.Contains($"{Uri.UriSchemeHttps}://")
            && entry.Service.Port > 0;
    }
}
