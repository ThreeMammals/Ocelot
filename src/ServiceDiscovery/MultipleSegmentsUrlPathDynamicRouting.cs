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
        int delimiter = upstreamUrlPath.IndexOf(ServiceNameDelimiter, 1);
        if (delimiter < 0)
        {
            return base.DownstreamPath(upstreamUrlPath);
        }

        var downstreamPath = upstreamUrlPath[(delimiter + 1)..];
        return downstreamPath.IsEmpty() || downstreamPath[0] == Slash
            ? downstreamPath.IsEmpty() ? Slash.ToString() : downstreamPath
            : string.Concat(Slash, downstreamPath);
    }

    public override ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        upstreamUrlPath ??= string.Empty;
        int delimiter = upstreamUrlPath.IndexOf(ServiceNameDelimiter, 1);
        if (delimiter < 0)
        {
            return base.GetServiceName(upstreamUrlPath);
        }

        ReadOnlySpan<char> serviceName = upstreamUrlPath.AsSpan(1, delimiter - 1).TrimEnd(Slash);
        return new(serviceName);
    }
}
