namespace Ocelot.DownstreamRouteFinder.Finder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class DownstreamRouteProviderFactory : IDownstreamRouteProviderFactory
    {
        private readonly Dictionary<string, IDownstreamRouteProvider> _providers;
        
        public DownstreamRouteProviderFactory(IServiceProvider provider)
        {
            _providers = provider.GetServices<IDownstreamRouteProvider>().ToDictionary(x => x.GetType().Name);
        }

        public IDownstreamRouteProvider Get(IInternalConfiguration config)
        {
            if(!config.ReRoutes.Any() && IsServiceDiscovery(config.ServiceProviderConfiguration))
            {
                return _providers[nameof(DownstreamRouteCreator)];
            }
                
            return _providers[nameof(DownstreamRouteFinder)];
        }

        private bool IsServiceDiscovery(ServiceProviderConfiguration config)
        {
            if(!string.IsNullOrEmpty(config?.Host) || config?.Port > 0)
            {
                return true;
            }

            return false;
        }
    }
}
