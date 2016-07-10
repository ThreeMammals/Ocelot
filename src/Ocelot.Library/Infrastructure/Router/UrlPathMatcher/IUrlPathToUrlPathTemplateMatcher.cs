namespace Ocelot.Library.Infrastructure.Router.UrlPathMatcher
{
     public interface IUrlPathToUrlPathTemplateMatcher
     {
        bool Match(string urlPath, string urlPathTemplate);
     }
} 