using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Provider;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.QueryStrings.Middleware;

namespace Ocelot.LoadBalancer.Middleware
{
    public class LoadBalancingMiddleware : OcelotMiddleware
    {
        private readonly IOcelotConfigurationProvider _configProvider;
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly ILoadBalancerHouse _loadBalancerHouse;

        public LoadBalancingMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            ILoadBalancerHouse loadBalancerHouse,
            IOcelotConfigurationProvider configProvider) 
            : base(requestScopedDataRepository)
        {
            _configProvider = configProvider;
            _next = next;
            _logger = loggerFactory.CreateLogger<QueryStringBuilderMiddleware>();
            _loadBalancerHouse = loadBalancerHouse;
        }

        public async Task Invoke(HttpContext context)
        {
            var configuration = await _configProvider.Get(); 

            var loadBalancer = await _loadBalancerHouse.Get(DownstreamRoute.ReRoute, configuration.Data.ServiceProviderConfiguration);
            if(loadBalancer.IsError)
            {
                _logger.LogDebug("there was an error retriving the loadbalancer, setting pipeline error");
                SetPipelineError(loadBalancer.Errors);
                return;
            }

            var hostAndPort = await loadBalancer.Data.Lease();
            if(hostAndPort.IsError)
            {
                _logger.LogDebug("there was an error leasing the loadbalancer, setting pipeline error");
                SetPipelineError(hostAndPort.Errors);
                return;
            }

            var uriBuilder = new UriBuilder(DownstreamRequest.RequestUri);
            uriBuilder.Host = hostAndPort.Data.DownstreamHost;
            if (hostAndPort.Data.DownstreamPort > 0)
            {
                uriBuilder.Port = hostAndPort.Data.DownstreamPort;
            }
            DownstreamRequest.RequestUri = uriBuilder.Uri;

            try
            {
                await _next.Invoke(context);
            }
            catch (Exception)
            {
                _logger.LogDebug("Exception calling next middleware, exception will be thrown to global handler");
                throw;
            }
            finally
            {
                loadBalancer.Data.Release(hostAndPort.Data);
            }
        }
    }
}
