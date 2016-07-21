namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
     public interface IUrlPathToUrlPathTemplateMatcher
     {
        UrlPathMatch Match(string downstreamUrlPath, string downStreamUrlPathTemplate);
     }
} 