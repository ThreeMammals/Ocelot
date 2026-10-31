using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Request.Middleware;
using Ocelot.Values;

namespace Ocelot.DownstreamUrlCreator;

public interface IDownstreamPathFormatter
{
    bool TryFormat(
        DownstreamRequest downstreamRequest,
        DownstreamRoute downstreamRoute,
        IInternalConfiguration configuration,
        List<PlaceholderNameAndValue> placeholders,
        DownstreamPath downstreamPath);
}
