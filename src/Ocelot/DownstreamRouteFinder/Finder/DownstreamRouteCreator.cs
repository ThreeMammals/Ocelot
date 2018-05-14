namespace Ocelot.DownstreamRouteFinder.Finder
{
    using System.Collections.Generic;
    using Configuration;
    using Configuration.Builder;
    using Configuration.Creator;
    using Configuration.File;
    using LoadBalancer.LoadBalancers;
    using Responses;
    using UrlMatcher;

    public class DownstreamRouteCreator : IDownstreamRouteProvider
    {
        private readonly IQoSOptionsCreator _qoSOptionsCreator;

        public DownstreamRouteCreator(IQoSOptionsCreator qoSOptionsCreator)
        {
            _qoSOptionsCreator = qoSOptionsCreator;
        }

        public Response<DownstreamRoute> Get(string upstreamUrlPath, string upstreamHttpMethod, IInternalConfiguration configuration, string upstreamHost)
        {
            var serviceName = GetServiceName(upstreamUrlPath);

            var downstreamPath = GetDownstreamPath(upstreamUrlPath);

            if(HasQueryString(downstreamPath))
            {
                downstreamPath = RemoveQueryString(downstreamPath);
            }

            var downstreamPathForKeys = $"/{serviceName}{downstreamPath}";

            var loadBalancerKey = CreateLoadBalancerKey(downstreamPathForKeys, upstreamHttpMethod, configuration.LoadBalancerOptions);

            var qosOptions = _qoSOptionsCreator.Create(configuration.QoSOptions, downstreamPathForKeys, new []{ upstreamHttpMethod });

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithServiceName(serviceName)
                .WithLoadBalancerKey(loadBalancerKey)
                .WithDownstreamPathTemplate(downstreamPath)
                .WithUseServiceDiscovery(true)
                .WithHttpHandlerOptions(configuration.HttpHandlerOptions)
                .WithQosOptions(qosOptions)
                .WithDownstreamScheme(configuration.DownstreamScheme)
                .WithLoadBalancerOptions(configuration.LoadBalancerOptions)
                .Build();

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(downstreamReRoute)
                .WithUpstreamHttpMethod(new List<string>(){ upstreamHttpMethod })
                .Build();

            return new OkResponse<DownstreamRoute>(new DownstreamRoute(new List<PlaceholderNameAndValue>(), reRoute));
        }

        private static string RemoveQueryString(string downstreamPath)
        {
            return downstreamPath
                .Substring(0, downstreamPath.IndexOf('?'));
        }

        private static bool HasQueryString(string downstreamPath)
        {
            return downstreamPath.Contains("?");
        }

        private static string GetDownstreamPath(string upstreamUrlPath)
        {
            return upstreamUrlPath
                .Substring(upstreamUrlPath.IndexOf('/', 1));
        }

        private static string GetServiceName(string upstreamUrlPath)
        {
            return upstreamUrlPath
                .Substring(1, upstreamUrlPath.IndexOf('/', 1))
                .TrimEnd('/');
        }

        private string CreateLoadBalancerKey(string downstreamTemplatePath, string httpMethod, LoadBalancerOptions loadBalancerOptions)
        {
            if (!string.IsNullOrEmpty(loadBalancerOptions.Type) && !string.IsNullOrEmpty(loadBalancerOptions.Key) && loadBalancerOptions.Type == nameof(CookieStickySessions))
            {
                return $"{nameof(CookieStickySessions)}:{loadBalancerOptions.Key}";
            }

            return CreateQoSKey(downstreamTemplatePath, httpMethod);
        }

        private string CreateQoSKey(string downstreamTemplatePath, string httpMethod)
        {
            var loadBalancerKey = $"{downstreamTemplatePath}|{httpMethod}";
            return loadBalancerKey;
        }
    }
}
