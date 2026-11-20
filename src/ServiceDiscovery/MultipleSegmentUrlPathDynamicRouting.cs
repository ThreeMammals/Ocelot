using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Ocelot.ServiceDiscovery;

/// <summary>
/// Dynamic routing utilities that use multiple URL path segments as a service name.
/// </summary>
/// <param name="contextAccessor">The current <see cref="HttpContext"/> to access.</param>
/// <param name="options">User defined options.</param>
public class MultipleSegmentUrlPathDynamicRouting(
    IHttpContextAccessor contextAccessor,
    IOptions<MultipleSegmentUrlPathDynamicRoutingOptions> options = null)
    : UrlPathDynamicRouting(contextAccessor)
{
    public const char Percent = '%';
    public const char DefaultDelimiter = '$';
    protected virtual char ServiceNameDelimiter => options?.Value?.Delimiter ?? DefaultDelimiter;

    protected override string DownstreamPath(string upstreamUrlPath)
    {
        ReadOnlySpan<char> path = DecodeUrlPath(upstreamUrlPath);
        if (path.Length <= 1)
            return SlashStr;

        int delimiter = FindDelimiter(path);
        if (delimiter < 0)
            return base.DownstreamPath(path.ToString());

        var downstreamPath = path[++delimiter..];
        return downstreamPath.Length == 0 || downstreamPath[0] == Slash
            ? downstreamPath.Length == 0 ? SlashStr : downstreamPath.ToString()
            : string.Concat(SlashStr.AsSpan(), downstreamPath); // ensure downstream path starts with a slash
    }

    public override ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        ReadOnlySpan<char> path = DecodeUrlPath(upstreamUrlPath);
        if (path.Length <= 1)
            return base.GetServiceName(path.ToString());

        int delimiter = FindDelimiter(path);
        if (delimiter < 0)
            return base.GetServiceName(path.ToString());

        ReadOnlySpan<char> service = path[1..delimiter].TrimEnd(Slash);
        int dot = service.IndexOf(Dot);
        return dot != -1
            ? new(service[(dot + 1)..], service[..dot])
            : new(service);
    }

    protected virtual int FindDelimiter(ReadOnlySpan<char> upstreamUrlPath)
    {
        char delimiter = ServiceNameDelimiter == Slash
            ? DefaultDelimiter
            : ServiceNameDelimiter;
        int index = upstreamUrlPath[1..].IndexOf(delimiter);
        return index < 0 ? -1 : ++index;
    }

    protected virtual ReadOnlySpan<char> DecodeUrlPath(string upstreamUrlPath)
    {
        if (upstreamUrlPath is null)
            return [];

        ReadOnlySpan<char> path = upstreamUrlPath.AsSpan();
        return path.IndexOf(Percent) < 0
            ? path
            : Uri.UnescapeDataString(upstreamUrlPath).AsSpan();
    }
}

public class MultipleSegmentUrlPathDynamicRoutingOptions
{
    public MultipleSegmentUrlPathDynamicRoutingOptions() => Delimiter = MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter;
    public MultipleSegmentUrlPathDynamicRoutingOptions(char delimiter) => Delimiter = delimiter;

    public char Delimiter { get; set; } = MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter;
}
