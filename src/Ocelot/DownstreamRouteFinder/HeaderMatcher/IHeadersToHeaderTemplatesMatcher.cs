using Ocelot.Values;
using System.Collections.Generic;

namespace Ocelot.DownstreamRouteFinder.HeaderMatcher;

public interface IHeadersToHeaderTemplatesMatcher
{
    bool Match(Dictionary<string, string> upstreamHeaders, Dictionary<string, UpstreamHeaderTemplate> routeHeaders);
}
