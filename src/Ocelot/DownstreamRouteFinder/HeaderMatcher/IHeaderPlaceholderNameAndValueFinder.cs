using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;
using System.Collections.Generic;

namespace Ocelot.DownstreamRouteFinder.HeaderMatcher;

public interface IHeaderPlaceholderNameAndValueFinder
{
    List<PlaceholderNameAndValue> Find(Dictionary<string, string> upstreamHeaders, Dictionary<string, UpstreamHeaderTemplate> templateHeaders);
}
