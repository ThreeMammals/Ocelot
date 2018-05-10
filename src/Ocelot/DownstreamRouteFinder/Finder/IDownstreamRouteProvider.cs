using System;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public interface IDownstreamRouteProvider
    {
        Response<DownstreamRoute> Get(string upstreamUrlPath, string upstreamHttpMethod, IInternalConfiguration configuration, string upstreamHost);
    }

    public class DownstreamRouteCreator : IDownstreamRouteProvider
    {
        public Response<DownstreamRoute> Get(string upstreamUrlPath, string upstreamHttpMethod, IInternalConfiguration configuration, string upstreamHost)
        {
            throw new NotImplementedException();
        }
    }

    public interface IDownstreamRouteProviderFactory
    {
        IDownstreamRouteProvider Get(IInternalConfiguration config);
    }

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
                
            return _providers[nameof(DownstreamRouteProvider)];
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
