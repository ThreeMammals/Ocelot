using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Configuration.Creator;
using Ocelot.Requester.QoS;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class ReRoute
    {
        public ReRoute(List<DownstreamReRoute> downstreamReRoute, 
            PathTemplate upstreamPathTemplate, 
            List<HttpMethod> upstreamHttpMethod, 
            UpstreamPathTemplate upstreamTemplatePattern, 
            string upstreamHost)
        {
            UpstreamHost = upstreamHost;
            DownstreamReRoute = downstreamReRoute;
            UpstreamPathTemplate = upstreamPathTemplate;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
        }

        public PathTemplate UpstreamPathTemplate { get; private set; }
        public UpstreamPathTemplate UpstreamTemplatePattern { get; private set; }
        public List<HttpMethod> UpstreamHttpMethod { get; private set; }
        public string UpstreamHost { get; private set; }
        public List<DownstreamReRoute> DownstreamReRoute { get; private set; }
    }
}
