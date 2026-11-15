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
    protected virtual string ServiceNameEnding => string.Empty;

    protected override string DownstreamPath(string upstreamUrlPath)
    {
        upstreamUrlPath = DecodeUrlPath(upstreamUrlPath);
        if (upstreamUrlPath.Length <= 1)
            return Slash.ToString();

        int delimiter = FindServiceNameDelimiter(upstreamUrlPath, out int delimiterLength);
        if (delimiter < 0)
            return base.DownstreamPath(upstreamUrlPath);

        var downstreamPath = upstreamUrlPath[(delimiter + delimiterLength)..];
        return downstreamPath.IsEmpty() || downstreamPath[0] == Slash
            ? downstreamPath.IsEmpty() ? Slash.ToString() : downstreamPath
            : string.Concat(Slash, downstreamPath); // ensure downstream path starts with a slash
    }

    public override ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        upstreamUrlPath = DecodeUrlPath(upstreamUrlPath);
        if (upstreamUrlPath.Length <= 1)
            return base.GetServiceName(upstreamUrlPath);

        int delimiter = FindServiceNameDelimiter(upstreamUrlPath, out _);
        if (delimiter < 0)
            return base.GetServiceName(upstreamUrlPath);

        ReadOnlySpan<char> service = upstreamUrlPath
            .AsSpan(1, delimiter - 1).TrimEnd(Slash);
        int dot = service.IndexOf(Dot);
        return dot != -1
            ? new(service[(dot + 1)..], service[..dot])
            : new(service);
    }

    private int FindServiceNameDelimiter(string upstreamUrlPath, out int delimiterLength)
    {
        string delimiterString = ServiceNameEnding?.Replace(Slash.ToString(), string.Empty);
        if (delimiterString.IsNotEmpty())
        {
            delimiterLength = delimiterString.Length;
            int index = upstreamUrlPath.AsSpan(1).IndexOf(delimiterString.AsSpan());
            return index < 0 ? -1 : index + 1;
        }

        delimiterLength = 1;
        char delimiter = ServiceNameDelimiter == Slash
            ? '$'
            : ServiceNameDelimiter;
        int charIndex = upstreamUrlPath.AsSpan(1).IndexOf(delimiter);
        return charIndex < 0 ? -1 : charIndex + 1;
    }

    private static string DecodeUrlPath(string upstreamUrlPath)
        => Uri.UnescapeDataString(upstreamUrlPath ?? string.Empty);
}
