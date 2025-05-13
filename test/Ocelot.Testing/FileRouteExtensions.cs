using Ocelot.Configuration.File;

namespace Ocelot.Testing;

public static class FileRouteExtensions
{
    public static FileRoute WithHosts(this FileRoute route, params FileHostAndPort[] hosts)
    {
        route.DownstreamHostAndPorts.AddRange(hosts);
        return route;
    }

    public static FileRoute WithPriority(this FileRoute route, int priority)
    {
        route.Priority = priority;
        return route;
    }

    public static FileRoute WithMethods(this FileRoute route, params string[] methods)
    {
        route.UpstreamHttpMethod.AddRange(methods);
        return route;
    }

    public static FileRoute WithUpstreamHeaderTransform(this FileRoute route, params KeyValuePair<string, string>[] pairs)
    {
        route.UpstreamHeaderTransform = new(pairs);
        return route;
    }
    public static FileRoute WithUpstreamHeaderTransform(this FileRoute route, string key, string value)
    {
        route.UpstreamHeaderTransform.TryAdd(key, value);
        return route;
    }

    public static FileRoute WithHttpHandlerOptions(this FileRoute route, FileHttpHandlerOptions options)
    {
        route.HttpHandlerOptions = options;
        return route;
    }

    public static FileRoute WithKey(this FileRoute route, string? key)
    {
        route.Key = key;
        return route;
    }

    public static FileRoute WithUpstreamHost(this FileRoute route, string? upstreamHost)
    {
        route.UpstreamHost = upstreamHost;
        return route;
    }
}
