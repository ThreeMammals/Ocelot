using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.HeaderMatcher;

/// <summary>
/// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-headers">Routing based on request header</see>.
/// </summary>
public interface IHeaderPlaceholderNameAndValueFinder
{
    IList<PlaceholderNameAndValue> Find(IDictionary<string, string> upstreamHeaders, IDictionary<string, UpstreamHeaderTemplate> templateHeaders);
}
