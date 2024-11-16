﻿using Ocelot.Configuration.File;

namespace Ocelot.Testing;

public static class FileRouteExtensions
{
    public static FileRoute WithHosts(this FileRoute route, params FileHostAndPort[] hosts)
    {
        route.DownstreamHostAndPorts.AddRange(hosts);
        return route;
    }

    public static FileRoute SetPriority(this FileRoute route, int priority)
    {
        route.Priority = priority;
        return route;
    }

    public static FileRoute WithMethods(this FileRoute route, params string[] methods)
    {
        route.UpstreamHttpMethod.AddRange(methods);
        return route;
    }
}
