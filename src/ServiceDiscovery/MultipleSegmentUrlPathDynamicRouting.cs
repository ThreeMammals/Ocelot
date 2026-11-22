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
    protected virtual char ServiceNameDelimiter
    {
        get
        {
            // Detect delimiter from the header first
            if (Request.Headers.TryGetValue(Headers.Ocelot_DynamicRouting_UrlPath_Delimiter, out var hdr)
                && hdr.TryGetNonEnumeratedCount(out int count) && count > 0)
            {
                var decoded = Decode(hdr).Trim();
                return decoded[0];
            }
            else
            {
                return options?.Value?.Delimiter ?? DefaultDelimiter;
            }
        }
    }

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
        // Detect delimiter position via number of segments from the header first
        if (Request.Headers.TryGetValue(Headers.Ocelot_DynamicRouting_UrlPath_Segments, out var segments)
            && int.TryParse(segments, out int segmentCount))
        {
            if (segmentCount < 1)
                return 0; // should return full path as downstream path, thus no service name detected
            else if (segmentCount == 1)
                return -1; // will fallback to base class implementation aka first-segment

            // Count slashes
            int pos = upstreamUrlPath[0] == Slash ? 1 : 0,
                count = 0;
            while (pos < upstreamUrlPath.Length && count < segmentCount)
            {
                if (upstreamUrlPath[pos++] == Slash)
                    count++;
            }
            return pos != upstreamUrlPath.Length
                ? --pos // happy path, wanted slash position was found
                : -1; // abnormal error, either segmentCount is greater than actual number of slashes, or path is too short, thus fallback to base class implementation
        }

        // No header, detect delimiter position
        char delimiter = ServiceNameDelimiter == Slash
            ? DefaultDelimiter
            : ServiceNameDelimiter;
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
    public MultipleSegmentUrlPathDynamicRoutingOptions() => Delimiter = MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter;
    public MultipleSegmentUrlPathDynamicRoutingOptions(char delimiter) => Delimiter = delimiter;

    public char Delimiter { get; set; } = MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter;
}
