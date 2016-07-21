namespace Ocelot.Library.Infrastructure.HostUrlRepository
{
    public class HostUrlMap
    {
        public HostUrlMap(string urlPathTemplate, string upstreamHostUrl)
        {
            UrlPathTemplate = urlPathTemplate;
            UpstreamHostUrl = upstreamHostUrl;
        }

        public string UrlPathTemplate {get;private set;}
        public string UpstreamHostUrl {get;private set;}
    }
}