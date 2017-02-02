using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.QueryStrings.Middleware;
using Ocelot.ServiceDiscovery;

namespace Ocelot.LoadBalancer.Middleware
{
    public class LoadBalancingMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly ILoadBalancerHouse _loadBalancerHouse;

        public LoadBalancingMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            ILoadBalancerHouse loadBalancerHouse) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<QueryStringBuilderMiddleware>();
            _loadBalancerHouse = loadBalancerHouse;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling query string builder middleware");

            var loadBalancer = _loadBalancerHouse.Get($"{DownstreamRoute.ReRoute.UpstreamTemplate}{DownstreamRoute.ReRoute.UpstreamHttpMethod}");
            //todo check reponse and return error
            
            var response = loadBalancer.Data.Lease();
            //todo check reponse and return error
            
            SetHostAndPortForThisRequest(response.Data);
            _logger.LogDebug("calling next middleware");

            //todo - try next middleware if we get an exception make sure we release 
            //the host and port? Not sure if this is the way to go but we shall see!
            try
            {
                await _next.Invoke(context);

                loadBalancer.Data.Release(response.Data);
            }
            catch (Exception)
            {
                loadBalancer.Data.Release(response.Data);
                throw;
            }

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}
