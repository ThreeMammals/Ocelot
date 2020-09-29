namespace Ocelot.Configuration
{
    using Ocelot.Values;
    using System.Collections.Generic;
    using System.Net.Http;

    public class Route
    {
        public Route(List<DownstreamRoute> downstreamRoute,
            IEnumerable<AggregateRouteConfig> aggregateRouteConfigs,
            List<HttpMethod> upstreamHttpMethod,
            UpstreamPathTemplate upstreamTemplatePattern,
            string upstreamHost,
            string aggregator)
        {
            UpstreamHost = upstreamHost;
            DownstreamRoute = downstreamRoute;
            AggregateRouteConfigs = aggregateRouteConfigs;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            Aggregator = aggregator;
        }

        public UpstreamPathTemplate UpstreamTemplatePattern { get; private set; }
        public List<HttpMethod> UpstreamHttpMethod { get; private set; }
        public string UpstreamHost { get; private set; }
        public List<DownstreamRoute> DownstreamRoute { get; private set; }
        public IEnumerable<AggregateRouteConfig> AggregateRouteConfigs { get; private set; }
        public string Aggregator { get; private set; }
    }
}
