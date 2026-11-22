using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ocelot.Infrastructure.Extensions;

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
    public static class Headers
    {
        public const string Ocelot_DynamicRouting_UrlPath_Delimiter = "Ocelot-DynamicRouting-UrlPath-Delimiter";
        public const string Ocelot_DynamicRouting_UrlPath_Segments = "Ocelot-DynamicRouting-UrlPath-Segments";
    }

    public const char Percent = '%';
    public const char DefaultDelimiter = '$';
    protected virtual char ServiceNameDelimiter => options?.Value?.Delimiter ?? DefaultDelimiter;

    protected override string DownstreamPath(string upstreamUrlPath)
    {
        ReadOnlySpan<char> path = Decode(upstreamUrlPath);
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
        ReadOnlySpan<char> path = Decode(upstreamUrlPath);
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
        char delimiter = ServiceNameDelimiter;
        if (options?.Value?.IsHeaderManagement == true)
        {
            if (Request.Headers.TryGetValue(Headers.Ocelot_DynamicRouting_UrlPath_Segments, out var segments)
                && int.TryParse(segments, out int segmentCount) && segmentCount > 0)
            {
                int count = 0;
                for (int pos = 1; pos < upstreamUrlPath.Length; pos++)
                {
                    if (upstreamUrlPath[pos] == Slash && ++count == segmentCount)
                        return pos;
                }
                return -1;
            }

            if (Request.Headers.TryGetValue(Headers.Ocelot_DynamicRouting_UrlPath_Delimiter, out var header))
            {
                var decoded = Decode(header);
                if (!decoded.IsEmpty && decoded[0] != Slash)
                    delimiter = decoded[0];
            }
        }

        if (delimiter == Slash)
            delimiter = DefaultDelimiter;
        int index = upstreamUrlPath[1..].IndexOf(delimiter);
        return index < 0 ? -1 : ++index;
    }

    protected virtual ReadOnlySpan<char> Decode(string value)
    {
        if (value.IsEmpty())
            return [];

        ReadOnlySpan<char> val = value.AsSpan();
        return val.IndexOf(Percent) < 0
            ? val
            : Uri.UnescapeDataString(value).AsSpan();
    }
}

public class MultipleSegmentUrlPathDynamicRoutingOptions
{
    public MultipleSegmentUrlPathDynamicRoutingOptions()
        => Delimiter = MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter;
    public MultipleSegmentUrlPathDynamicRoutingOptions(char delimiter)
        => Delimiter = delimiter;
    public MultipleSegmentUrlPathDynamicRoutingOptions(bool isHeaderManagement)
        => IsHeaderManagement = isHeaderManagement;

    public MultipleSegmentUrlPathDynamicRoutingOptions(char delimiter, bool isHeaderManagement)
    {
        Delimiter = delimiter;
        IsHeaderManagement = isHeaderManagement;
    }

    public char Delimiter { get; set; } = MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter;
    public bool IsHeaderManagement { get; set; } = false;
}
