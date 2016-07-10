namespace Ocelot.Library.Infrastructure.UrlPathMatcher
{
     public interface IUrlPathToUrlPathTemplateMatcher
     {
        bool Match(string urlPath, string urlPathTemplate);
     }
} 