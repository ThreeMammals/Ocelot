using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Values;
using System.Linq;
using Ocelot.Configuration.Creator;
using System;

namespace Ocelot.Configuration.Builder
{
    public class ReRouteBuilder
    {
        private string _upstreamTemplate;
        private UpstreamPathTemplate _upstreamTemplatePattern;
        private List<HttpMethod> _upstreamHttpMethod;
        private string _upstreamHost;
        private List<DownstreamReRoute> _downstreamReRoutes;
        private string _aggregator;

        public ReRouteBuilder()
        {
            _downstreamReRoutes = new List<DownstreamReRoute>();
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

        public ReRouteBuilder WithUpstreamPathTemplate(string input)
        {
            _upstreamTemplate = input;
            return this;
        }

        public ReRouteBuilder WithUpstreamTemplatePattern(UpstreamPathTemplate input)
        {
            _upstreamTemplatePattern = input;
            return this;
        }

        public ReRouteBuilder WithUpstreamHttpMethod(List<string> input)
        {
            _upstreamHttpMethod = (input.Count == 0) ? new List<HttpMethod>() : input.Select(x => new HttpMethod(x.Trim())).ToList();
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
                new PathTemplate(_upstreamTemplate), 
                _upstreamHttpMethod, 
                _upstreamTemplatePattern, 
                _upstreamHost,
                _aggregator
                );
        }
    }
}
