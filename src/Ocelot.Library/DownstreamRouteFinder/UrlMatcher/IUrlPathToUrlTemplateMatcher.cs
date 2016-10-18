using Ocelot.Library.Responses;

namespace Ocelot.Library.DownstreamRouteFinder.UrlMatcher
{
    public interface IUrlPathToUrlTemplateMatcher
     {
        Response<UrlMatch> Match(string upstreamUrlPath, string upstreamUrlPathTemplate);
     }
} 