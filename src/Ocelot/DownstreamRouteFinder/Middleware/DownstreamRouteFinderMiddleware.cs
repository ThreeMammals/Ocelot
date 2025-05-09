using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder.Middleware;

public class DownstreamRouteFinderMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDownstreamRouteProviderFactory _factory;

    public DownstreamRouteFinderMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IDownstreamRouteProviderFactory downstreamRouteFinder)
        : base(loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>())
    {
        _next = next;
        _factory = downstreamRouteFinder;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var upstreamUrlPath = httpContext.Request.Path.ToString();
        var upstreamQueryString = httpContext.Request.QueryString.ToString();
        var internalConfiguration = httpContext.Items.IInternalConfiguration();
        var hostHeader = httpContext.Request.Headers.Host.ToString();
        var upstreamHost = hostHeader.Contains(':')
            ? hostHeader.Split(':')[0]
            : hostHeader;
        var upstreamHeaders = httpContext.Request.Headers
            .ToDictionary(h => h.Key, h => string.Join(';', (IList<string>)h.Value));

        Logger.LogDebug(() => $"Upstream URL path: {upstreamUrlPath}");

        var provider = _factory.Get(internalConfiguration);
        var response = provider.Get(upstreamUrlPath, upstreamQueryString, httpContext.Request.Method, internalConfiguration, upstreamHost, upstreamHeaders);
        if (response.IsError)
        {
            Logger.LogWarning(() => $"{MiddlewareName} setting pipeline errors because {provider.GetType().Name} returned the following ->{response.Errors.ToErrorString(true)}");
            httpContext.Items.UpsertErrors(response.Errors);
            return;
        }

        Logger.LogDebug(() => $"Downstream templates: {string.Join(", ", response.Data.Route.DownstreamRoute.Select(r => r.DownstreamPathTemplate.Value))}");

        // why set both of these on HttpContext
        httpContext.Items.UpsertTemplatePlaceholderNameAndValues(response.Data.TemplatePlaceholderNameAndValues);
        httpContext.Items.UpsertDownstreamRoute(response.Data);

        await _next.Invoke(httpContext);
    }
}
