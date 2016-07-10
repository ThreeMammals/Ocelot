namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
     public interface IUrlPathToUrlPathTemplateMatcher
     {
        UrlPathMatch Match(string urlPath, string urlPathTemplate);
     }
} 