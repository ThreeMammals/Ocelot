using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder;

public class DownstreamRouteFinderMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDownstreamRouteProviderFactory _factory;

    public DownstreamRouteFinderMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory, IDownstreamRouteProviderFactory providerFactory)
        : base(loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>())
    {
        _next = next;
        _factory = providerFactory;
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
        var upstreamHeaders = httpContext.Request.Headers;

        Logger.LogDebug(() => $"Upstream URL path: {upstreamUrlPath}");

        var provider = _factory.Get(internalConfiguration);
        var response = provider.Get(upstreamUrlPath, upstreamQueryString, httpContext.Request.Method, internalConfiguration, upstreamHost, upstreamHeaders);
        if (response.IsError)
        {
            Logger.LogWarning(() => $"{MiddlewareName} setting pipeline errors because {provider.GetType().Name} returned the following ->{response.Errors.ToErrorString(true)}");
            httpContext.Items.UpsertErrors(response.Errors);
            return;
        }

        Logger.LogDebug(() => $"Downstream templates: {string.Join(", ", response.Data.DownstreamRoute.Select(r => r.DownstreamPathTemplate.Value))}");

        // why set both of these on HttpContext, asked Mr. Tom LoL :)
        httpContext.Items.UpsertRoute(response.Data);
        httpContext.Items.UpsertTemplatePlaceholderNameAndValues(response.Data.TemplatePlaceholderNameAndValues);

        await _next.Invoke(httpContext);
    }
}
