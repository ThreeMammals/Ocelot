using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.HeaderMatcher;

public class HeadersToHeaderTemplatesMatcher : IHeadersToHeaderTemplatesMatcher
{
    public bool Match(IHeaderDictionary upstreamHeaders, IDictionary<string, UpstreamHeaderTemplate> routeHeaders)
    {
        bool IsMatching(KeyValuePair<string, UpstreamHeaderTemplate> h)
        {
            return upstreamHeaders.TryGetValue(h.Key, out StringValues header)
                && routeHeaders[h.Key].Pattern.IsMatch(header);
        }
        return routeHeaders == null || upstreamHeaders != null && routeHeaders.All(IsMatching);
    }
}
