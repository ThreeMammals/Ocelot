namespace Ocelot.Library.Infrastructure.UrlPathTemplateRepository
{
    public class UrlPathTemplateMap
    {
        public UrlPathTemplateMap(string downstreamUrlPathTemplate, string upstreamUrlPathTemplate)
        {
            DownstreamUrlPathTemplate = downstreamUrlPathTemplate;
            UpstreamUrlPathTemplate = upstreamUrlPathTemplate;
        }

        public string DownstreamUrlPathTemplate {get;private set;}
        public string UpstreamUrlPathTemplate {get;private set;}
    }
}