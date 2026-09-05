using Microsoft.AspNetCore.Http;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.LoadBalancer;

public class LoadBalancingMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoadBalancerHouse _loadBalancerHouse;

    public LoadBalancingMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        ILoadBalancerHouse loadBalancerHouse)
        : base(loggerFactory.CreateLogger<LoadBalancingMiddleware>())
    {
        _next = next;
        _loadBalancerHouse = loadBalancerHouse;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var downstreamRoute = httpContext.Items.DownstreamRoute();

        var internalConfiguration = httpContext.Items.IInternalConfiguration();

        var loadBalancer = _loadBalancerHouse.Get(downstreamRoute, internalConfiguration.ServiceProviderConfiguration);

        if (loadBalancer.IsError)
        {
            httpContext.Items.UpsertErrors(loadBalancer.Errors);
            return;
        }

        var hostAndPort = await loadBalancer.Data.LeaseAsync(httpContext);
        if (hostAndPort.IsError)
        {
            httpContext.Items.UpsertErrors(hostAndPort.Errors);
            return;
        }

        var downstreamRequest = httpContext.Items.DownstreamRequest();

        //todo check downstreamRequest is ok
        downstreamRequest.Host = hostAndPort.Data.DownstreamHost;

        if (hostAndPort.Data.DownstreamPort > 0)
        {
            downstreamRequest.Port = hostAndPort.Data.DownstreamPort;
        }

        if (!string.IsNullOrEmpty(hostAndPort.Data.Scheme))
        {
            downstreamRequest.Scheme = hostAndPort.Data.Scheme;
        }

        try
        {
            // If an exception occurs, the object will be handled by the global exception handler
            await _next.Invoke(httpContext);
        }
        finally
        {
            loadBalancer.Data.Release(hostAndPort.Data);
        }
    }
}
