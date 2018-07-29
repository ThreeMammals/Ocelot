namespace Ocelot.DownstreamRouteFinder.Finder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Logging;

    public class DownstreamRouteProviderFactory : IDownstreamRouteProviderFactory
    {
        private readonly Dictionary<string, IDownstreamRouteProvider> _providers;
        private IOcelotLogger _logger;
        
        public DownstreamRouteProviderFactory(IServiceProvider provider, IOcelotLoggerFactory factory)
        {
            _logger = factory.CreateLogger<DownstreamRouteProviderFactory>();
            _providers = provider.GetServices<IDownstreamRouteProvider>().ToDictionary(x => x.GetType().Name);
        }

        public IDownstreamRouteProvider Get(IInternalConfiguration config)
        {
            //todo - this is a bit hacky we are saying there are no reRoutes or there are reRoutes but none of them have
            //an upstream path template which means they are dyanmic and service discovery is on...
            if((!config.ReRoutes.Any() || config.ReRoutes.All(x => string.IsNullOrEmpty(x.UpstreamPathTemplate.Value))) && IsServiceDiscovery(config.ServiceProviderConfiguration))
            {
                _logger.LogInformation($"Selected {nameof(DownstreamRouteCreator)} as DownstreamRouteProvider for this request");
                return _providers[nameof(DownstreamRouteCreator)];
            }
                
            return _providers[nameof(DownstreamRouteFinder)];
        }

        private bool IsServiceDiscovery(ServiceProviderConfiguration config)
        {
            if(!string.IsNullOrEmpty(config?.Host) && config?.Port > 0 && !string.IsNullOrEmpty(config.Type))
            {
                return true;
            }

            return false;
        }
    }
}
