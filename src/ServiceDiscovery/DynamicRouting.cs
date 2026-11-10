using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.Extensions;
using System.Runtime.CompilerServices;

namespace Ocelot.ServiceDiscovery;

public class DynamicRouting : IDynamicRouting
{
    public const char Dot = '.';
    public const char Slash = '/';

    private readonly IHttpContextAccessor _contextAccessor;
    public DynamicRouting(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Dynamic Routing upstream path.
    /// </summary>
    public static string UpstreamPath(string path = "/", [CallerMemberName] string serviceName = null, string serviceNamespace = null)
    {
        var pth = path.TrimStart('/');
        return serviceNamespace.IsEmpty()
            ? $"/{serviceName}/{pth}" // first segment is always Service Name
            : $"/{serviceNamespace}.{serviceName}/{pth}";
    }

    public string GetDownstreamPath()
        => GetDownstreamPath(_contextAccessor.HttpContext.Request.Path);

    public string GetDownstreamPath(PathString upstreamUrlPath)
        => GetDownstreamPath(upstreamUrlPath.ToString());

    /// <summary>
    /// Gets the downstream path part of the URL without the service name.
    /// Thus, the first segment of the upstream URL is removed.
    /// </summary>
    /// <param name="upstreamUrlPath">The URL with the service name in the first segment.</param>
    /// <returns>A <see cref="string"/> object containing the downstream path.</returns>
    public string GetDownstreamPath(string upstreamUrlPath)
        => DownstreamPath(upstreamUrlPath);

    protected virtual string DownstreamPath(string upstreamUrlPath)
        => Path(upstreamUrlPath);

    public static string Path(string upstreamUrlPath)
    {
        int index = upstreamUrlPath.IndexOf(Slash, 1);
        return index != -1
            ? upstreamUrlPath[index..]
            : Slash.ToString();
    }
}

public interface IDynamicRouting
{
    string GetDownstreamPath();
    string GetDownstreamPath(PathString upstreamUrlPath);
    string GetDownstreamPath(string upstreamUrlPath);
}
