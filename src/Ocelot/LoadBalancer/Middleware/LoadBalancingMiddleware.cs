using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using System;
using System.Threading.Tasks;

namespace Ocelot.LoadBalancer.Middleware
{
    public class LoadBalancingMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly ILoadBalancerHouse _loadBalancerHouse;

        public LoadBalancingMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            ILoadBalancerHouse loadBalancerHouse)
                : base(loggerFactory.CreateLogger<LoadBalancingMiddleware>())
        {
            _next = next;
            _loadBalancerHouse = loadBalancerHouse;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var loadBalancer = _loadBalancerHouse.Get(context.DownstreamReRoute, context.Configuration.ServiceProviderConfiguration);
            if (loadBalancer.IsError)
            {
                Logger.LogDebug("there was an error retriving the loadbalancer, setting pipeline error");
                SetPipelineError(context, loadBalancer.Errors);
                return;
            }

            var hostAndPort = await loadBalancer.Data.Lease(context);
            if (hostAndPort.IsError)
            {
                Logger.LogDebug("there was an error leasing the loadbalancer, setting pipeline error");
                SetPipelineError(context, hostAndPort.Errors);
                return;
            }

            context.DownstreamRequest.Host = hostAndPort.Data.DownstreamHost;

            if (hostAndPort.Data.DownstreamPort > 0)
            {
                context.DownstreamRequest.Port = hostAndPort.Data.DownstreamPort;
            }

            if (!string.IsNullOrEmpty(hostAndPort.Data.Scheme))
            {
                context.DownstreamRequest.Scheme = hostAndPort.Data.Scheme;
            }

            try
            {
                await _next.Invoke(context);
            }
            catch (Exception)
            {
                Logger.LogDebug("Exception calling next middleware, exception will be thrown to global handler");
                throw;
            }
            finally
            {
                loadBalancer.Data.Release(hostAndPort.Data);
            }
        }
    }
}
