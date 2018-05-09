using System;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    //todo rename IDownstreamRouteProvider
    public interface IDownstreamRouteFinder
    {
        Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod, IInternalConfiguration configuration, string upstreamHost);
    }

    public class DownstreamRouteCreator : IDownstreamRouteFinder
    {
        public Response<DownstreamRoute> FindDownstreamRoute(string upstreamUrlPath, string upstreamHttpMethod, IInternalConfiguration configuration, string upstreamHost)
        {
            throw new NotImplementedException();
        }
    }

    //todo - rename IDownstreamRouteProviderFactory
    public interface IDownstreamRouteFinderFactory
    {
        IDownstreamRouteFinder Get(IInternalConfiguration config);
    }

    //todo - rename DownstreamRouteProviderFactory
    public class DownstreamRouteFinderFactory : IDownstreamRouteFinderFactory
    {
        private readonly Dictionary<string, IDownstreamRouteFinder> _providers;
        
        public DownstreamRouteFinderFactory(IServiceProvider provider)
        {
            _providers = provider.GetServices<IDownstreamRouteFinder>().ToDictionary(x => x.GetType().Name);
        }

        public IDownstreamRouteFinder Get(IInternalConfiguration config)
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
