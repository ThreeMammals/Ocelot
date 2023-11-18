using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator
{
    public interface IDownstreamPathPlaceholderReplacer
    {
        Response<DownstreamPath> Replace(string downstreamPathTemplate, List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues);
    }
}
