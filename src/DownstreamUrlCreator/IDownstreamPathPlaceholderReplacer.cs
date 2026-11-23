using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator;

public interface IDownstreamPathPlaceholderReplacer
{
    DownstreamPath Replace(string downstreamPathTemplate, IReadOnlyList<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues);
}
