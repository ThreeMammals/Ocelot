﻿using System;
using System.Collections.Generic;
using System.Linq;

using Ocelot.Configuration;

using Ocelot.Logging;

using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public class DownstreamRouteProviderFactory : IDownstreamRouteProviderFactory
    {
        private readonly Dictionary<string, IDownstreamRouteProvider> _providers;
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
            if ((!config.Routes.Any() || config.Routes.All(x => string.IsNullOrEmpty(x.UpstreamTemplatePattern?.OriginalValue))) && IsServiceDiscovery(config.ServiceProviderConfiguration))
            {
                _logger.LogInformation($"Selected {nameof(DownstreamRouteCreator)} as DownstreamRouteProvider for this request");
                return _providers[nameof(DownstreamRouteCreator)];
            }

            return _providers[nameof(DownstreamRouteFinder)];
        }

        private static bool IsServiceDiscovery(ServiceProviderConfiguration config)
        {
            return !string.IsNullOrEmpty(config?.Host) && config?.Port > 0 && !string.IsNullOrEmpty(config.Type);
        }
    }
}
