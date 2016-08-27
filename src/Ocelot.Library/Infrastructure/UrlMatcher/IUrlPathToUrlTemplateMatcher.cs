namespace Ocelot.Library.Infrastructure.UrlMatcher
{
     public interface IUrlPathToUrlTemplateMatcher
     {
        UrlMatch Match(string downstreamUrlPath, string downstreamUrlTemplate);
     }
} 