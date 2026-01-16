using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher;

public class RegExUrlMatcher : IUrlPathToUrlTemplateMatcher
{
    public UrlMatch Match(string upstreamUrlPath, string upstreamQueryString, UpstreamPathTemplate pathTemplate)
        => !pathTemplate.ContainsQueryString
            ? new(pathTemplate.Pattern.IsMatch(upstreamUrlPath))
            : new(pathTemplate.Pattern.IsMatch($"{upstreamUrlPath}{upstreamQueryString}"));
}
