using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher;

public interface IUrlPathToUrlTemplateMatcher
{
    UrlMatch Match(string upstreamUrlPath, string upstreamQueryString, UpstreamPathTemplate pathTemplate);
}
