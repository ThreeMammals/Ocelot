namespace Ocelot.Library.UrlMatcher
{
    using Responses;

    public interface IUrlPathToUrlTemplateMatcher
     {
        Response<UrlMatch> Match(string upstreamUrlPath, string upstreamUrlPathTemplate);
     }
} 