namespace Ocelot.Configuration.Builder
{
    using Ocelot.Configuration.File;
    using Ocelot.Values;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

    public class ReRouteBuilder
    {
        private UpstreamPathTemplate _upstreamTemplatePattern;
        private List<HttpMethod> _upstreamHttpMethod;
        private string _upstreamHost;
        private List<DownstreamReRoute> _downstreamReRoutes;
        private List<AggregateReRouteConfig> _downstreamReRoutesConfig;
        private string _aggregator;

        public ReRouteBuilder()
        {
            _downstreamReRoutes = new List<DownstreamReRoute>();
            _downstreamReRoutesConfig = new List<AggregateReRouteConfig>();
        }

        public ReRouteBuilder WithDownstreamReRoute(DownstreamReRoute value)
        {
            _downstreamReRoutes.Add(value);
            return this;
        }

        public ReRouteBuilder WithDownstreamReRoutes(List<DownstreamReRoute> value)
        {
            _downstreamReRoutes = value;
            return this;
        }

        public ReRouteBuilder WithUpstreamHost(string upstreamAddresses)
        {
            _upstreamHost = upstreamAddresses;
            return this;
        }

        public ReRouteBuilder WithUpstreamPathTemplate(UpstreamPathTemplate input)
        {
            _upstreamTemplatePattern = input;
            return this;
        }

        public ReRouteBuilder WithUpstreamHttpMethod(List<string> input)
        {
            _upstreamHttpMethod = (input.Count == 0) ? new List<HttpMethod>() : input.Select(x => new HttpMethod(x.Trim())).ToList();
            return this;
        }

        public ReRouteBuilder WithAggregateReRouteConfig(List<AggregateReRouteConfig> aggregateReRouteConfigs)
        {
            _downstreamReRoutesConfig = aggregateReRouteConfigs;
            return this;
        }

        public ReRouteBuilder WithAggregator(string aggregator)
        {
            _aggregator = aggregator;
            return this;
        }

        public ReRoute Build()
        {
            return new ReRoute(
                _downstreamReRoutes,
                _downstreamReRoutesConfig,
                _upstreamHttpMethod,
                _upstreamTemplatePattern,
                _upstreamHost,
                _aggregator
                );
        }
    }
}
