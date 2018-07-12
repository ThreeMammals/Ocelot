using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.UrlMatcher
{
    public interface IUrlPathToUrlTemplateMatcher
     {
        Response<UrlMatch> Match(string upstreamUrlPath, string upstreamQueryString, string upstreamUrlPathTemplate, bool containsQueryString);
     }
} 
