using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.ServiceDiscovery;

/// <summary>
/// Default dynamic routing utilities based on the "URL path first segment" as a service name.
/// </summary>
/// <remarks>It supports service namespaces as a prefix to a dot character that delimits the namespace from the service name.<br/>
/// The namespace value <b>must not</b> contain dots at all; thus, only the service name is allowed to have dots in its value.
/// <para>Example: the <c>http://ocelot.net/namespace.service-name/path</c> URL.</para>
/// </remarks>
/// <param name="contextAccessor">The current <see cref="HttpContext"/> to access.</param>
public class UrlPathDynamicRouting(IHttpContextAccessor contextAccessor) : IDynamicRouting
{
    public const char Dot = '.';
    public const char Slash = '/';

    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;

    public HttpRequest Request => _contextAccessor.HttpContext.Request;

    public virtual string GetDownstreamPath()
        => DownstreamPath(Request.Path);

    /// <summary>
    /// Gets the downstream path part of the URL without the service name.
    /// Thus, the first segment of the upstream URL is removed.
    /// </summary>
    /// <param name="request">The request URL with the service name in the first segment.</param>
    /// <returns>A <see cref="string"/> object containing the downstream path.</returns>
    public virtual string GetDownstreamPath(HttpRequest request)
        => DownstreamPath(request.Path);

    protected virtual string DownstreamPath(string upstreamUrlPath)
    {
        int index = upstreamUrlPath.IndexOf(Slash, 1);
        return index != -1
            ? upstreamUrlPath[index..]
            : Slash.ToString();
    }

    public virtual ServiceInfo GetServiceName()
        => GetServiceName(Request.Path);

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

    public virtual object SetServiceName(object state, ServiceInfo service)
        => SetServiceName(state.ToString(), service.Name, service.Namespace);

    protected virtual string SetServiceName(string path, string serviceName, string serviceNamespace = null)
        => UpstreamPath(path, serviceName, serviceNamespace);

    /// <summary>
    /// Creates Dynamic Routing upstream path.
    /// </summary>
    public static string UpstreamPath(string path, string serviceName, string serviceNamespace = null)
    {
        path ??= Slash.ToString();
        var pth = path.TrimStart(Slash);
        return serviceNamespace.IsEmpty()
            ? string.Concat(Slash, serviceName, Slash, pth) // $"/{serviceName}/{pth}"
            : string.Concat(Slash, serviceNamespace, Dot, serviceName, Slash, pth); // $"/{serviceNamespace}.{serviceName}/{pth}";
    }
}
