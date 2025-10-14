using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;

namespace Ocelot.DownstreamRouteFinder.Finder;

public class DownstreamRouteProviderFactory : IDownstreamRouteProviderFactory
{
    private readonly Dictionary<string, IDownstreamRouteProvider> _providers; // TODO We need to use a HashSet<int> here for quicker lookups
    private readonly IOcelotLogger _logger;

    public DownstreamRouteProviderFactory(IServiceProvider provider, IOcelotLoggerFactory factory)
    {
        _logger = factory.CreateLogger<DownstreamRouteProviderFactory>();
        _providers = provider.GetServices<IDownstreamRouteProvider>().ToDictionary(x => x.GetType().Name);
    }

    public IDownstreamRouteProvider Get(IInternalConfiguration config)
    {
        //todo - this is a bit hacky we are saying there are no routes or there are routes but none of them have
        //an upstream path template which means they are dyanmic and service discovery is on...
        if ((config.Routes.Count == 0 || config.Routes.All(x => string.IsNullOrEmpty(x.UpstreamTemplatePattern?.OriginalValue))) && IsServiceDiscovery(config.ServiceProviderConfiguration))
        {
            _logger.LogInformation($"Selected {nameof(DiscoveryDownstreamRouteFinder)} as {nameof(IDownstreamRouteProvider)} for this request");

            return _providers[nameof(DiscoveryDownstreamRouteFinder)];
        }

        return _providers[nameof(DownstreamRouteFinder)];
    }

    private static bool IsServiceDiscovery(ServiceProviderConfiguration config)
    {
        return !string.IsNullOrEmpty(config?.Host) && config?.Port > 0 && !string.IsNullOrEmpty(config?.Type);
    }
}
