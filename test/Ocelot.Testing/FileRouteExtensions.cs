using Ocelot.Configuration.File;

namespace Ocelot.Testing;

public static class FileRouteExtensions
{
    public static FileRoute WithHosts(this FileRoute route, params FileHostAndPort[] hosts)
    {
        route.DownstreamHostAndPorts.AddRange(hosts);
        return route;
    }
}
