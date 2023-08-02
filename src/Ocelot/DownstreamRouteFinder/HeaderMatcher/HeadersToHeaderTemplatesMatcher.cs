using Ocelot.Values;
using System.Collections.Generic;
using System.Linq;

namespace Ocelot.DownstreamRouteFinder.HeaderMatcher;

public class HeadersToHeaderTemplatesMatcher : IHeadersToHeaderTemplatesMatcher
{
    public bool Match(Dictionary<string, string> upstreamHeaders, Dictionary<string, UpstreamHeaderTemplate> routeHeaders)
    {
        return routeHeaders == null ||
            upstreamHeaders != null && routeHeaders.All(
                h => upstreamHeaders.ContainsKey(h.Key) && routeHeaders[h.Key].Pattern.IsMatch(upstreamHeaders[h.Key]));
    }
}
