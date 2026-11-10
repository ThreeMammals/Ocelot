using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
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

    public async Task Invoke(HttpContext context)
    {
        Logger.LogDebug(() => $"Upstream URL path: {context.Request.Path}");

        var configuration = context.Items.IInternalConfiguration();
        var provider = _factory.Get(configuration);
        if (!TryGetRoute(provider, context.Request, out Route route, out IEnumerable<Error> errors))
        {
            Logger.LogWarning(() => $"{MiddlewareName} setting pipeline errors because {provider.GetType().Name} returned the following ->{errors.ToErrorString(true)}");
            context.Items.UpsertErrors((List<Error>)errors);
            return;
        }

        Logger.LogDebug(() => $"Downstream templates: {string.Join(", ", route.DownstreamRoute.Select(r => r.DownstreamPathTemplate.Value))}");

        // why set both of these on HttpContext, asked Mr. Tom LoL :)
        context.Items.UpsertRoute(route);
        context.Items.UpsertTemplatePlaceholderNameAndValues(route.TemplatePlaceholderNameAndValues);

        await _next.Invoke(context);
    }

    protected virtual bool TryGetRoute(IDownstreamRouteProvider provider, HttpRequest request, out Route route, out IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(request);

        var configuration = request.HttpContext.Items.IInternalConfiguration();
        var upstreamHost = UpstreamHost(request);
        var response = provider.Get(request.Path, request.QueryString.ToString(), request.Method, configuration, upstreamHost, request.Headers);
        route = response.Data;
        errors = response.Errors;
        return !response.IsError;
    }

    private string UpstreamHost(HttpRequest request)
    {
        var host = request.Headers.Host.ToString();
        return host.Contains(':')
            ? host.Split(':')[0]
            : host;
    }
}
