namespace Ocelot.Library.Infrastructure.UrlMatcher
{
     public interface IUrlPathToUrlTemplateMatcher
     {
        UrlMatch Match(string upstreamUrlPath, string upstreamUrlTemplate);
     }
} 