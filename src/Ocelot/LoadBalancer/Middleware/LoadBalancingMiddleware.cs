using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.QueryStrings.Middleware;

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
            _logger.LogDebug("started calling load balancing middleware");

            var loadBalancer = _loadBalancerHouse.Get(DownstreamRoute.ReRoute.ReRouteKey);
            if(loadBalancer.IsError)
            {
                SetPipelineError(loadBalancer.Errors);
                return;
            }

            var hostAndPort = await loadBalancer.Data.Lease();
            if(hostAndPort.IsError)
            { 
                SetPipelineError(hostAndPort.Errors);
                return;
            }

            SetHostAndPortForThisRequest(hostAndPort.Data);

            _logger.LogDebug("calling next middleware");

            try
            {
                await _next.Invoke(context);

                loadBalancer.Data.Release(hostAndPort.Data);
            }
            catch (Exception)
            {
                loadBalancer.Data.Release(hostAndPort.Data);
                 
                 _logger.LogDebug("error calling next middleware, exception will be thrown to global handler");
                throw;
            }

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}
