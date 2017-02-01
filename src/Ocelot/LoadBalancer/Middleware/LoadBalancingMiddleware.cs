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

        public LoadBalancingMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<QueryStringBuilderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling query string builder middleware");

            //todo - get out of di? or do this when we bootstrap?
            var serviceProviderFactory = new ServiceProviderFactory();
            var serviceConfig = new ServiceConfiguraion(
                DownstreamRoute.ReRoute.ServiceName,
                DownstreamRoute.ReRoute.DownstreamHost,
                DownstreamRoute.ReRoute.DownstreamPort,
                DownstreamRoute.ReRoute.UseServiceDiscovery);
            //todo - get this out of some kind of service provider house?
            var serviceProvider = serviceProviderFactory.Get(serviceConfig);

            //todo - get out of di? or do this when we bootstrap?
            var loadBalancerFactory = new LoadBalancerFactory(serviceProvider);
            //todo - currently instanciates a load balancer per request which is wrong, 
            //need some kind of load balance house! :)
            var loadBalancer = loadBalancerFactory.Get(DownstreamRoute.ReRoute.ServiceName, DownstreamRoute.ReRoute.LoadBalancer);
            var response = loadBalancer.Lease();

            _logger.LogDebug("calling next middleware");

            //todo - try next middleware if we get an exception make sure we release 
            //the host and port? Not sure if this is the way to go but we shall see!
            try
            {
                await _next.Invoke(context);

                loadBalancer.Release(response.Data);
            }
            catch (Exception exception)
            {
                loadBalancer.Release(response.Data);
                throw;
            }

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}
