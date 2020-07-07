using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class Route
    {
        public Route(List<DownstreamRoute> downstreamRoute,
            List<AggregateRouteConfig> downstreamRouteConfig,
            List<HttpMethod> upstreamHttpMethod,
            UpstreamPathTemplate upstreamTemplatePattern,
            string upstreamHost,
            string aggregator,
            Dictionary<string, string> upstreamHeaders)
        {
            UpstreamHost = upstreamHost;
            DownstreamRoute = downstreamRoute;
            DownstreamRouteConfig = downstreamRouteConfig;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            Aggregator = aggregator;
            UpstreamHeaders = upstreamHeaders;
        }

        public UpstreamPathTemplate UpstreamTemplatePattern { get; set; }
        public List<HttpMethod> UpstreamHttpMethod { get; set; }
        public string UpstreamHost { get; set; }
        public List<DownstreamRoute> DownstreamRoute { get; set; }
        public List<AggregateRouteConfig> DownstreamRouteConfig { get; set; }
        public string Aggregator { get; set; }
        public Dictionary<string, string> UpstreamHeaders { get; set; }
    }
}
