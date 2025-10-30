using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Logging;

namespace Ocelot.Configuration.Creator;

public class HttpHandlerOptionsCreator : IHttpHandlerOptionsCreator
{
    private readonly IOcelotTracer _tracer;
    public HttpHandlerOptionsCreator(IServiceProvider services)
        => _tracer = services.GetService<IOcelotTracer>();

    public HttpHandlerOptions Create(FileHttpHandlerOptions options)
    {
        options ??= new();
        bool hasTracer = _tracer != null;
        return new(options, hasTracer);
    }

    public HttpHandlerOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.HttpHandlerOptions, globalConfiguration.HttpHandlerOptions);
    }

    public HttpHandlerOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.HttpHandlerOptions, globalConfiguration.HttpHandlerOptions);
    }

    protected virtual HttpHandlerOptions Create(IRouteGrouping grouping, FileHttpHandlerOptions options, FileGlobalHttpHandlerOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(grouping);
        var group = globalOptions;
        bool isGlobal = group?.RouteKeys is null || // undefined section or array option -> is global
            group.RouteKeys.Count == 0 || // empty collection -> is global
            group.RouteKeys.Contains(grouping.Key); // this route is in the group
        bool hasTracer = _tracer != null;
        if (options == null && globalOptions != null && isGlobal)
        {
            return new(globalOptions, hasTracer);
        }

        if (options != null && globalOptions == null)
        {
            return new(options, hasTracer);
        }
        else if (options != null && globalOptions != null && !isGlobal)
        {
            return new(options, hasTracer);
        }

        if (options != null && globalOptions != null && isGlobal)
        {
            return Merge(options, globalOptions);
        }

        return new();
    }

    protected virtual HttpHandlerOptions Merge(FileHttpHandlerOptions options, FileHttpHandlerOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(globalOptions);
        options.AllowAutoRedirect ??= globalOptions.AllowAutoRedirect ?? false;
        options.MaxConnectionsPerServer ??= globalOptions.MaxConnectionsPerServer ?? int.MaxValue;
        options.PooledConnectionLifetimeSeconds ??= globalOptions.PooledConnectionLifetimeSeconds ?? HttpHandlerOptions.DefaultPooledConnectionLifetimeSeconds;
        options.UseCookieContainer ??= globalOptions.UseCookieContainer ?? false;
        options.UseProxy ??= globalOptions.UseProxy ?? false;
        options.UseTracing ??= globalOptions.UseTracing ?? false;
        var useTracing = _tracer != null && options.UseTracing.Value;
        return new(options, useTracing);
    }
}
