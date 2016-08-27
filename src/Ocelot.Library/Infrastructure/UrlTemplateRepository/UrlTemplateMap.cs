namespace Ocelot.Library.Infrastructure.UrlTemplateRepository
{
    public class UrlTemplateMap
    {
        public UrlTemplateMap(string downstreamUrlTemplate, string upstreamUrlPathTemplate)
        {
            DownstreamUrlTemplate = downstreamUrlTemplate;
            UpstreamUrlPathTemplate = upstreamUrlPathTemplate;
        }

        public string DownstreamUrlTemplate {get;private set;}
        public string UpstreamUrlPathTemplate {get;private set;}
    }
}