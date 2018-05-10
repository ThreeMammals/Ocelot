using System;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;

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
            var serviceName = upstreamUrlPath
                .Substring(1, upstreamUrlPath.IndexOf('/', 1))
                .TrimEnd('/');

            var downstreamPath = upstreamUrlPath
                .Substring(upstreamUrlPath.IndexOf('/', 1));

            if(downstreamPath.Contains("?"))
            {
                downstreamPath = downstreamPath
                    .Substring(0, downstreamPath.IndexOf('?'));
            }

            var key = CreateReRouteKey(upstreamUrlPath, upstreamHttpMethod);

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithServiceName(serviceName)
                .WithReRouteKey(key)
                .WithDownstreamPathTemplate(downstreamPath)
                .Build();

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(downstreamReRoute)
                .WithUpstreamHttpMethod(new List<string>(){ upstreamHttpMethod })
                .Build();

            return new OkResponse<DownstreamRoute>(new DownstreamRoute(new List<PlaceholderNameAndValue>(), reRoute));
        
        }

        private string CreateReRouteKey(string downstreamTemplatePath, string httpMethod)
        {
            //note - not sure if this is the correct key, but this is probably the only unique key i can think of given my poor brain
            var loadBalancerKey = $"{downstreamTemplatePath}|{httpMethod}";
            return loadBalancerKey;
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
