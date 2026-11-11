using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;
using System.Runtime.CompilerServices;

namespace Ocelot.ServiceDiscovery;

public class DynamicRouting(IHttpContextAccessor contextAccessor) : IDynamicRouting
{
    public const char Dot = '.';
    public const char Slash = '/';

    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;

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

    public virtual string GetDownstreamPath()
        => GetDownstreamPath(_contextAccessor.HttpContext.Request.Path);

    public virtual string GetDownstreamPath(string upstreamUrlPath)
    {
        int index = upstreamUrlPath.IndexOf(Slash, 1);
        return index != -1
            ? upstreamUrlPath[index..]
            : Slash.ToString();
    }

    public virtual ServiceInfo GetServiceName()
        => GetServiceName(_contextAccessor.HttpContext.Request.Path);

    public virtual ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        ReadOnlySpan<char> path = upstreamUrlPath.IsNotEmpty()
            ? upstreamUrlPath.AsSpan()
            : [Slash];
        int index = path[1..].IndexOf(Slash);
        ReadOnlySpan<char> name = index == -1
            ? path[1..]
            : path.Slice(1, index).TrimEnd(Slash);

        index = name.IndexOf(Dot);
        return (index != -1)
            ? new(name[++index..], name[..--index])
            : new(name); // service namespace -> string.Empty
    }
}
