using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Ocelot.Configuration;

public class UpstreamRoutingHeaders
{
    public IReadOnlyDictionary<string, ICollection<string>> Headers { get; }

    public UpstreamRoutingHeaders(IReadOnlyDictionary<string, ICollection<string>> headers)
    {
        Headers = headers;
    }

    public bool Any() => Headers.Any();

    public bool HasAnyOf(IHeaderDictionary requestHeaders)
    {
        IHeaderDictionary normalizedHeaders = NormalizeHeaderNames(requestHeaders);
        foreach (var h in Headers)
        {
            if (normalizedHeaders.TryGetValue(h.Key, out var values) &&
                h.Value.Intersect(values, StringComparer.OrdinalIgnoreCase).Any())
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAllOf(IHeaderDictionary requestHeaders)
    {
        IHeaderDictionary normalizedHeaders = NormalizeHeaderNames(requestHeaders);
        foreach (var h in Headers)
        {
            if (!normalizedHeaders.TryGetValue(h.Key, out var values))
            {
                return false;
            }

            if (!h.Value.Intersect(values, StringComparer.OrdinalIgnoreCase).Any())
            {
                return false;
            }
        }

        return true;
    }

    private static IHeaderDictionary NormalizeHeaderNames(IHeaderDictionary headers)
    {
        var upperCaseHeaders = new HeaderDictionary();
        foreach (KeyValuePair<string, StringValues> kv in headers)
        {
            var key = kv.Key.ToUpperInvariant();
            upperCaseHeaders.Add(key, kv.Value);
        }

        return upperCaseHeaders;
    }
}
