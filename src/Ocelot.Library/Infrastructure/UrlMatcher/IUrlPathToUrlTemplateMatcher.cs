using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlMatcher
{
     public interface IUrlPathToUrlTemplateMatcher
     {
        Response<UrlMatch> Match(string upstreamUrlPath, string upstreamUrlPathTemplate);
     }
} 