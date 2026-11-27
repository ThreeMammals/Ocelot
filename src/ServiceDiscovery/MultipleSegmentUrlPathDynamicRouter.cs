using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.ServiceDiscovery;

/// <summary>
/// Dynamic routing utilities that use multiple URL path segments as a service name.
/// </summary>
/// <param name="contextAccessor">The current <see cref="HttpContext"/> to access.</param>
/// <param name="options">User defined options.</param>
public class MultipleSegmentUrlPathDynamicRouter(
    IHttpContextAccessor contextAccessor, IOptions<MultipleSegmentUrlPathDynamicRouterOptions> options = null)
    : UrlPathDynamicRouter(contextAccessor)
{
    public static class Headers
    {
        public const string Ocelot_DynamicRouting_UrlPath_Delimiter = "Ocelot-DynamicRouting-UrlPath-Delimiter";
        public const string Ocelot_DynamicRouting_UrlPath_Segments = "Ocelot-DynamicRouting-UrlPath-Segments";
    }

    public const char Percent = '%';
    public const char DefaultDelimiter = '$';
    protected virtual char ServiceNameDelimiter => options?.Value?.Delimiter ?? DefaultDelimiter;
    protected virtual int? Segments => options?.Value?.Segments;

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
        int? segments = Segments;
        if (options?.Value?.IsHeaderManagement == true)
        {
            if (Request.Headers.TryGetValue(Headers.Ocelot_DynamicRouting_UrlPath_Segments, out var headerSegments)
                && int.TryParse(headerSegments, out int headerSegmentCount) && headerSegmentCount >= 0)
            {
                segments = headerSegmentCount;
            }
        }

        if (segments > 0)
        {
            int count = 0;
            for (int pos = 1; pos < upstreamUrlPath.Length; pos++)
            {
                if (upstreamUrlPath[pos] == Slash && ++count == segments)
                    return pos;
            }
            return -1;
        }

        char delimiter = ServiceNameDelimiter;
        if (options?.Value?.IsHeaderManagement == true
            && Request.Headers.TryGetValue(Headers.Ocelot_DynamicRouting_UrlPath_Delimiter, out var header))
        {
            var decoded = Decode(header);
            if (!decoded.IsEmpty && decoded[0] != Slash)
                delimiter = decoded[0];
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

public class MultipleSegmentUrlPathDynamicRouterOptions
{
    public MultipleSegmentUrlPathDynamicRouterOptions()
        => Delimiter = MultipleSegmentUrlPathDynamicRouter.DefaultDelimiter;
    public MultipleSegmentUrlPathDynamicRouterOptions(char delimiter)
        => Delimiter = delimiter;
    public MultipleSegmentUrlPathDynamicRouterOptions(bool isHeaderManagement)
        => IsHeaderManagement = isHeaderManagement;

    public MultipleSegmentUrlPathDynamicRouterOptions(int segments)
        => Segments = segments;

    public MultipleSegmentUrlPathDynamicRouterOptions(char delimiter, bool isHeaderManagement)
    {
        Delimiter = delimiter;
        IsHeaderManagement = isHeaderManagement;
    }

    public char Delimiter { get; set; } = MultipleSegmentUrlPathDynamicRouter.DefaultDelimiter;
    public bool IsHeaderManagement { get; set; } = false;
    public int? Segments { get; set; }
}
