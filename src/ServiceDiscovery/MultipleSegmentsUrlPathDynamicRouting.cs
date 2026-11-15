using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.ServiceDiscovery;

/// <summary>
/// Dynamic routing utilities that use multiple URL path segments as a service name.
/// </summary>
/// <param name="contextAccessor">The current <see cref="HttpContext"/> to access.</param>
public class MultipleSegmentsUrlPathDynamicRouting(IHttpContextAccessor contextAccessor)
    : UrlPathDynamicRouting(contextAccessor)
{
    protected virtual char ServiceNameDelimiter => '$';

    protected override string DownstreamPath(string upstreamUrlPath)
    {
        upstreamUrlPath ??= string.Empty;
        if (upstreamUrlPath.Length <= 1)
            return Slash.ToString();

        int delimiter = upstreamUrlPath.IndexOf(ServiceNameDelimiter, 1);
        if (delimiter < 0)
            return base.DownstreamPath(upstreamUrlPath);

        var downstreamPath = upstreamUrlPath[(delimiter + 1)..];
        return downstreamPath.IsEmpty() || downstreamPath[0] == Slash
            ? downstreamPath.IsEmpty() ? Slash.ToString() : downstreamPath
            : string.Concat(Slash, downstreamPath); // ensure downstream path starts with a slash
    }

    public override ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        upstreamUrlPath ??= string.Empty;
        if (upstreamUrlPath.Length <= 1)
            return base.GetServiceName(upstreamUrlPath);

        int delimiter = upstreamUrlPath.IndexOf(ServiceNameDelimiter, 1);
        if (delimiter < 0)
            return base.GetServiceName(upstreamUrlPath);

        ReadOnlySpan<char> service = upstreamUrlPath
            .AsSpan(1, delimiter - 1).TrimEnd(Slash);
        int dot = service.IndexOf(Dot);
        return dot != -1
            ? new(service[(dot + 1)..], service[..dot])
            : new(service);
    }
}
