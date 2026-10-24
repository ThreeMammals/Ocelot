using Ocelot.Infrastructure.Extensions;
using System.Runtime.CompilerServices;

namespace Ocelot.ServiceDiscovery;

public static class DynamicRouting
{
    public const char Dot = '.';

    /// <summary>
    /// Dynamic Routing upstream path.
    /// </summary>
    public static string Path(string path = "/", [CallerMemberName] string serviceName = null, string serviceNamespace = null)
    {
        var pth = path.TrimStart('/');
        return serviceNamespace.IsEmpty()
            ? $"/{serviceName}/{pth}" // first segment is always Service Name
            : $"/{serviceNamespace}.{serviceName}/{pth}";
    }
}
