using System;
using System.Threading.Tasks;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.LoadBalancer.Providers;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.LoadBalancer.Middleware
{
    public class LoadBalancingMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly ILoadBalancerHouse _loadBalancerHouse;
        private readonly IDownstreamRequestBaseHostProvider _baseHostProvider;

        public LoadBalancingMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            ILoadBalancerHouse loadBalancerHouse,
            IDownstreamRequestBaseHostProvider baseHostProvider) 
                :base(loggerFactory.CreateLogger<LoadBalancingMiddleware>())
        {
            _next = next;
            _loadBalancerHouse = loadBalancerHouse;
            _baseHostProvider = baseHostProvider;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var loadBalancer = await _loadBalancerHouse.Get(context.DownstreamReRoute, context.Configuration.ServiceProviderConfiguration);
            if(loadBalancer.IsError)
            {
                Logger.LogDebug("there was an error retriving the loadbalancer, setting pipeline error");
                SetPipelineError(context, loadBalancer.Errors);
                return;
            }

            var hostAndPort = await loadBalancer.Data.Lease(context);
            if(hostAndPort.IsError)
            {
                Logger.LogDebug("there was an error leasing the loadbalancer, setting pipeline error");
                SetPipelineError(context, hostAndPort.Errors);
                return;
            }

            var baseHostInfo = _baseHostProvider.GetBaseHostInfo(hostAndPort.Data.DownstreamHost);
            context.DownstreamRequest.Host = baseHostInfo.BaseHost;
            context.DownstreamRequest.ApplicationName = baseHostInfo.ApplicationName;

            if (hostAndPort.Data.DownstreamPort > 0)
            {
                context.DownstreamRequest.Port = hostAndPort.Data.DownstreamPort;
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
