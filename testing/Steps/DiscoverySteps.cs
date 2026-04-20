using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Metadata;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Runtime.CompilerServices;

namespace Ocelot.Testing.Steps;

public class DiscoverySteps : ConcurrentSteps
{
    protected virtual string ServiceName([CallerMemberName] string? serviceName = null)
        => serviceName ?? GetType().Name;
    protected virtual string ServiceNamespace() => GetType().Namespace ?? string.Empty;

    public FileConfiguration GivenDynamicRouting(Dictionary<string, IEnumerable<string>> services, params FileDynamicRoute[] routes)
    {
        var config = new FileConfiguration()
        {
            DynamicRoutes = new(routes),
            GlobalConfiguration = new()
            {
                DownstreamScheme = Uri.UriSchemeHttp,
                ServiceDiscoveryProvider = new()
                {
                    Type = nameof(DynamicRoutingDiscoveryProvider),
                    Host = "doesn't matter for this provider", // it should not be empty due to DownstreamRouteProviderFactory.Get
                    Port = 1, // see DownstreamRouteProviderFactory.IsServiceDiscovery
                },
                LoadBalancerOptions = new(nameof(RoundRobin)),
            },
        };
        config.GlobalConfiguration.Metadata = services.ToDictionary(x => x.Key, x => x.Value.Csv());
        return config;
    }

    protected virtual void GivenDiscoveryMetadata(FileDynamicRoute route, int[] ports)
        => route.Metadata = new Dictionary<string, string>()
        {
            { route.ServiceName, ports.Select(DownstreamUrl).Csv() },
        };

    protected static readonly ServiceDiscoveryFinderDelegate DynamicRoutingDiscoveryFinder = (services, config, route)
        => new DynamicRoutingDiscoveryProvider(services, config, route);
    protected static void WithDiscovery(IServiceCollection services) => services
        .AddSingleton(DynamicRoutingDiscoveryFinder)
        .AddOcelot();
}

public class DynamicRoutingDiscoveryProvider : IServiceDiscoveryProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceProviderConfiguration _config;
    private readonly DownstreamRoute _downstreamRoute;

    public DynamicRoutingDiscoveryProvider(IServiceProvider serviceProvider, ServiceProviderConfiguration config, DownstreamRoute downstreamRoute)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _downstreamRoute = downstreamRoute;
    }

    public Task<List<Service>> GetAsync()
    {
        if (!_downstreamRoute.MetadataOptions.Metadata.TryGetValue(_downstreamRoute.ServiceName, out var data)
            || data.IsEmpty())
            return Task.FromResult<List<Service>>(new());

        var urls = _downstreamRoute
            .GetMetadata<string[]>(_downstreamRoute.ServiceName)
            .Select(x => new Uri(x))
            .ToList();
        var services = urls
            .Select(url => new Service(
                name: _downstreamRoute.ServiceName,
                hostAndPort: new(url.Host, url.Port, url.Scheme.IfEmpty(_downstreamRoute.DownstreamScheme)),
                id: $"{_downstreamRoute.ServiceNamespace}.{_downstreamRoute.ServiceName}",
                version: DateTime.UtcNow.ToString("O"),
                tags: Enumerable.Empty<string>()))
            .ToList();
        return Task.FromResult(services);
    }
}
