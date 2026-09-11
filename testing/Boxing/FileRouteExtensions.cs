using Ocelot.Configuration.File;

namespace Ocelot.Testing.Boxing;

/// <summary>
/// For type: <see cref="FileRoute"/>
/// </summary>
public static class FileRouteExtensions
{
    public static R WithHosts<R, H>(this R route, params H[] hosts)
        where R : FileRoute
        where H : FileHostAndPort
        => Box.In<FileRouteBox<R>, R>(route).Hosts(hosts).Out(); //route.DownstreamHostAndPorts.AddRange(hosts);

    public static R WithPriority<R>(this R route, int priority)
        where R : FileRoute
        => FileRouteBox<R>.In(route).Priority(priority).Out(); //route.Priority = priority;

    public static R WithMethods<R>(this R route, params string[] methods)
        where R : FileRoute
        => FileRouteBox.In(route).Methods(methods).Out(); //route.UpstreamHttpMethod = [.. methods];

    public static R WithUpstreamHeaderTransform<R>(this R route, params KeyValuePair<string, string>[] pairs)
        where R : FileRoute
        => new FileRouteBox<R>(route).UpstreamHeaderTransform(pairs).Out(); //route.UpstreamHeaderTransform = new Dictionary<string, string>(pairs);

    public static R WithUpstreamHeaderTransform<R>(this R route, string key, string value)
        where R : FileRoute
        => new FileRouteBox<R>(route).UpstreamHeaderTransform(key, value).Out(); //route.UpstreamHeaderTransform.TryAdd(key, value);

    public static R WithDownstreamHeaderTransform<R>(this R route, string key, string value)
        where R : FileRoute
        => new FileRouteBox<R>(route).DownstreamHeaderTransform(key, value).Out(); //route.DownstreamHeaderTransform.TryAdd(key, value);

    public static R WithHttpHandlerOptions<R, O>(this R route, O options)
        where R : FileRoute
        where O : FileHttpHandlerOptions
        => new FileRouteBox<R>(route).HandlerOptions(options).Out(); //route.HttpHandlerOptions = options;

    public static R WithKey<R>(this R route, string? key)
        where R : FileRoute
        => new FileRouteBox<R>(route).Key(key).Out(); //route.Key = key;

    public static R WithUpstreamHost<R>(this R route, string? upstreamHost)
        where R : FileRoute
        => new FileRouteBox<R>(route).UpstreamHost(upstreamHost).Out(); //route.UpstreamHost = upstreamHost;
}
