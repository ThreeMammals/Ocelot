﻿using Ocelot.Logging;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.Provider.Consul;

public class Consul : IServiceDiscoveryProvider
{
    private readonly ConsulRegistryConfiguration _configuration;
    private readonly IConsulClient _consul;
    private readonly IOcelotLogger _logger;
    private readonly IConsulServiceBuilder _serviceBuilder;

    public Consul(
        ConsulRegistryConfiguration config,
        IOcelotLoggerFactory factory,
        IConsulClientFactory clientFactory,
        IConsulServiceBuilder serviceBuilder)
    {
        _configuration = config;
        _consul = clientFactory.Get(_configuration);
        _logger = factory.CreateLogger<Consul>();
        _serviceBuilder = serviceBuilder;
    }

    public virtual async Task<List<Service>> GetAsync()
    {
        var entriesTask = _consul.Health.Service(_configuration.KeyOfServiceInConsul, string.Empty, true);
        var nodesTask = _consul.Catalog.Nodes();

        await Task.WhenAll(entriesTask, nodesTask);

        var entries = entriesTask.Result.Response ?? Array.Empty<ServiceEntry>();
        var nodes = nodesTask.Result.Response ?? Array.Empty<Node>();
        var services = new List<Service>();

        if (entries.Length != 0)
        {
            _logger.LogDebug(() => $"{nameof(Consul)} Provider: Found total {entries.Length} service entries for '{_configuration.KeyOfServiceInConsul}' service.");
            _logger.LogDebug(() => $"{nameof(Consul)} Provider: Found total {nodes.Length} catalog nodes.");
            var collection = BuildServices(entries, nodes);
            services.AddRange(collection);
        }
        else
        {
            _logger.LogWarning(() => $"{nameof(Consul)} Provider: No service entries found for '{_configuration.KeyOfServiceInConsul}' service!");
        }

        return services;
    }

    protected virtual IEnumerable<Service> BuildServices(ServiceEntry[] entries, Node[] nodes)
        => _serviceBuilder.BuildServices(entries, nodes);
}
