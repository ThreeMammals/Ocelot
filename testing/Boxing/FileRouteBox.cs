using Ocelot.Configuration.File;
using System.Collections;

namespace Ocelot.Testing.Boxing;

public static class FileRouteBox
{
    public static FileRouteBox<T> In<T>(T route) where T : FileRoute
        => new(route);
    public static FileRouteBox<T> With<T>(T route) where T : FileRoute
        => new(route);
}

public class FileRouteBox<T>(T route) : Box<T>(route, "Ocelot.Configuration.File.FileRoute")
    where T : FileRoute
{
    public static FileRouteBox<T> In(T route) => new(route);
    public static FileRouteBox<T> With(T route) => new(route);

    public FileRouteBox<T> Hosts<H>(params H[] hosts) where H : class // FileHostAndPort
    {
        //route.DownstreamHostAndPorts.AddRange(hosts);
        var property = Me.GetProperty("DownstreamHostAndPorts");
        IList? downstreamHostAndPorts = property?.GetValue(Instance) as IList;
        ArgumentNullException.ThrowIfNull(downstreamHostAndPorts);
        for (int i = 0; i < hosts.Length; i++)
        {
            var host = hosts[i];
            if (host?.GetType().FullName != "Ocelot.Configuration.File.FileHostAndPort")
                throw new ArgumentException($"Argument at index {i} is not type of Ocelot.Configuration.File.FileHostAndPort", nameof(hosts));
            downstreamHostAndPorts.Add(host);
        }
        return this;
    }

    public FileRouteBox<T> Priority(int priority)
    {
        //route.Priority = priority;
        var property = Me.GetProperty("Priority");
        property?.SetValue(Instance, priority);
        return this;
    }

    public FileRouteBox<T> Methods(params string[] methods)
    {
        //route.UpstreamHttpMethod = [.. methods];
        var property = Me.GetProperty("UpstreamHttpMethod") ?? throw new NullReferenceException("UpstreamHttpMethod");
        var collection = Activator.CreateInstance(property.PropertyType, (IEnumerable<string>)methods);
        property.SetValue(Instance, collection);
        return this;
    }

    public FileRouteBox<T> UpstreamHeaderTransform(params KeyValuePair<string, string>[] pairs)
    {
        //route.UpstreamHeaderTransform = new Dictionary<string, string>(pairs);
        var property = Me.GetProperty("UpstreamHeaderTransform");
        IDictionary<string, string> upstreamHeaderTransform = property?.GetValue(Instance) as IDictionary<string, string>
            ?? throw new ArgumentNullException(nameof(upstreamHeaderTransform));
        for (int i = 0; i < pairs.Length; i++)
        {
            var kv = pairs[i];
            upstreamHeaderTransform.Add(kv.Key, kv.Value);
        }
        return this;
    }

    public FileRouteBox<T> UpstreamHeaderTransform(string key, string value)
    {
        //route.UpstreamHeaderTransform.TryAdd(key, Instance);
        var property = Me.GetProperty("UpstreamHeaderTransform");
        IDictionary<string, string> upstreamHeaderTransform = property?.GetValue(Instance) as IDictionary<string, string>
            ?? throw new ArgumentNullException(nameof(upstreamHeaderTransform));
        upstreamHeaderTransform.TryAdd(key, value);
        return this;
    }

    public FileRouteBox<T> DownstreamHeaderTransform(string key, string value)
    {
        //route.DownstreamHeaderTransform.TryAdd(key, value);
        var property = Me.GetProperty("DownstreamHeaderTransform");
        IDictionary<string, string> downstreamHeaderTransform = property?.GetValue(Instance) as IDictionary<string, string>
            ?? throw new ArgumentNullException(nameof(downstreamHeaderTransform));
        downstreamHeaderTransform.TryAdd(key, value);
        return this;
    }

    public FileRouteBox<T> HandlerOptions<O>(O options) where O : class // FileHttpHandlerOptions
    {
        //route.HttpHandlerOptions = options;
        if (options?.GetType().FullName != "Ocelot.Configuration.File.FileHttpHandlerOptions")
            throw new ArgumentException($"Is not type of Ocelot.Configuration.File.FileHttpHandlerOptions", nameof(options));
        var property = Me.GetProperty("HttpHandlerOptions");
        property?.SetValue(Instance, options);
        return this;
    }

    public FileRouteBox<T> Key(string? key)
    {
        //route.Key = key;
        var property = Me.GetProperty("Key");
        property?.SetValue(Instance, key);
        return this;
    }

    public FileRouteBox<T> UpstreamHost(string? upstreamHost)
    {
        //route.UpstreamHost = upstreamHost;
        var property = Me.GetProperty("UpstreamHost");
        property?.SetValue(Instance, upstreamHost);
        return this;
    }
}
