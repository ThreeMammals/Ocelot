﻿namespace Ocelot.Configuration
{
    using System.Collections.Generic;
    using System.Net.Http;

    using File;

    using Values;

    public class Route
    {
        public Route(List<DownstreamRoute> downstreamRoute,
            List<AggregateRouteConfig> downstreamRouteConfig,
            List<HttpMethod> upstreamHttpMethod,
            UpstreamPathTemplate upstreamTemplatePattern,
            string upstreamHost,
            string aggregator)
        {
            UpstreamHost = upstreamHost;
            DownstreamRoute = downstreamRoute;
            DownstreamRouteConfig = downstreamRouteConfig;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            Aggregator = aggregator;
        }

        public UpstreamPathTemplate UpstreamTemplatePattern { get; }
        public List<HttpMethod> UpstreamHttpMethod { get; }
        public string UpstreamHost { get; }
        public List<DownstreamRoute> DownstreamRoute { get; }
        public List<AggregateRouteConfig> DownstreamRouteConfig { get; }
        public string Aggregator { get; }
    }
}
