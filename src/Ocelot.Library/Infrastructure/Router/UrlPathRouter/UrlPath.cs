namespace Ocelot.Library.Infrastructure.Router.UrlPathRouter
{
    public class UrlPath
    {
        public UrlPath(string downstreamUrlPathTemplate, string upstreamUrlPathTemplate)
        {
            DownstreamUrlPathTemplate = downstreamUrlPathTemplate;
            UpstreamUrlPathTemplate = upstreamUrlPathTemplate;
        }

        public string DownstreamUrlPathTemplate {get;private set;}
        public string UpstreamUrlPathTemplate {get;private set;}
    }
}