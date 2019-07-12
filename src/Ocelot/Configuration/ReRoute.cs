namespace Ocelot.Configuration
{
    using Ocelot.Configuration.File;
    using Ocelot.Values;
    using System.Collections.Generic;
    using System.Net.Http;

    public class ReRoute
    {
        public ReRoute(List<DownstreamReRoute> downstreamReRoute,
            List<AggregateReRouteConfig> downstreamReRouteConfig,
            List<HttpMethod> upstreamHttpMethod,
            UpstreamPathTemplate upstreamTemplatePattern,
            string upstreamHost,
            string aggregator,
            UpstreamHeaderRoutingOptions upstreamHeaderRoutingOptions)
        {
            UpstreamHost = upstreamHost;
            DownstreamReRoute = downstreamReRoute;
            DownstreamReRouteConfig = downstreamReRouteConfig;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            Aggregator = aggregator;
            UpstreamHeaderRoutingOptions = upstreamHeaderRoutingOptions;
        }

        public UpstreamPathTemplate UpstreamTemplatePattern { get; private set; }
        public List<HttpMethod> UpstreamHttpMethod { get; private set; }
        public string UpstreamHost { get; private set; }
        public List<DownstreamReRoute> DownstreamReRoute { get; private set; }
        public List<AggregateReRouteConfig> DownstreamReRouteConfig { get; private set; }
        public string Aggregator { get; private set; }
        public UpstreamHeaderRoutingOptions UpstreamHeaderRoutingOptions { get; private set; }
    }
}
