namespace Ocelot.DownstreamRouteFinder.Finder
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Configuration;
    using Configuration.Builder;
    using Configuration.Creator;
    using Configuration.File;
    using LoadBalancer.LoadBalancers;
    using Ocelot.Configuration.Repository;
    using Ocelot.DynamicConfigurationProvider;
    using Ocelot.Logging;
    using Responses;
    using UrlMatcher;

    public class DownstreamRouteCreator : IDownstreamRouteProvider
    {
        private readonly IQoSOptionsCreator _qoSOptionsCreator;
        private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
        private readonly IDynamicConfigurationProviderFactory _factory;
        private readonly IOcelotLogger _logger;
        private readonly ConcurrentDictionary<string, OkResponse<DownstreamRoute>> _cache;

        public DownstreamRouteCreator(IQoSOptionsCreator qoSOptionsCreator,
            IRateLimitOptionsCreator rateLimitOptionsCreator,
            IDynamicConfigurationProviderFactory factory,
            IOcelotLoggerFactory loggerFactory)
        {
            _qoSOptionsCreator = qoSOptionsCreator;
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
            _factory = factory;
            _logger = loggerFactory.CreateLogger<DownstreamRouteCreator>();
            _cache = new ConcurrentDictionary<string, OkResponse<DownstreamRoute>>();
        }

        public async Task<Response<DownstreamRoute>> GetAsync(string upstreamUrlPath, string upstreamQueryString, string upstreamHttpMethod, IInternalConfiguration configuration, string upstreamHost)
        {            
            var serviceName = GetServiceName(upstreamUrlPath);

            var downstreamPath = GetDownstreamPath(upstreamUrlPath);

            if (HasQueryString(downstreamPath))
            {
                downstreamPath = RemoveQueryString(downstreamPath);
            }

            var downstreamPathForKeys = CreateDownstreamPathForKeys(serviceName, downstreamPath);

            var loadBalancerKey = CreateLoadBalancerKey(downstreamPathForKeys, upstreamHttpMethod, configuration.LoadBalancerOptions);

            if (_cache.TryGetValue(loadBalancerKey + "", out var downstreamRoute))
            {
                return downstreamRoute;
            }
            
            var downstreamReRoute = await SetUpDownstreamReRouteAsync(serviceName, loadBalancerKey, downstreamPath, upstreamHttpMethod, configuration);

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(downstreamReRoute)
                .WithUpstreamHttpMethod(new List<string>() { upstreamHttpMethod })
                .Build();

            downstreamRoute = new OkResponse<DownstreamRoute>(new DownstreamRoute(new List<PlaceholderNameAndValue>(), reRoute));

            _cache.AddOrUpdate(loadBalancerKey, downstreamRoute, (x, y) => downstreamRoute);

            return downstreamRoute;
        }

        private async Task<DownstreamReRoute> SetUpDownstreamReRouteAsync(string serviceName, string loadBalancerKey, 
                                                            string downstreamPath, string upstreamHttpMethod,
                                                            IInternalConfiguration configuration)
        {
            var downstreamPathForKeys = CreateDownstreamPathForKeys(serviceName, downstreamPath);

            var qosOptions = _qoSOptionsCreator.Create(configuration.QoSOptions, downstreamPathForKeys, new[] { upstreamHttpMethod });

            var reRoute = await GetDynamicRouteConfigurationAsync(serviceName, configuration);
            
            var enableRateLimiting = reRoute?.RateLimitOptions?.EnableRateLimiting == true;

            var rateLimitOptions = _rateLimitOptionsCreator.Create(reRoute, configuration, enableRateLimiting);

            //TODO: add other options as well

            return new DownstreamReRouteBuilder()
                .WithServiceName(serviceName)
                .WithLoadBalancerKey(loadBalancerKey)
                .WithDownstreamPathTemplate(downstreamPath)
                .WithUseServiceDiscovery(true)
                .WithEnableRateLimiting(enableRateLimiting)
                .WithRateLimitOptions(rateLimitOptions)
                .WithHttpHandlerOptions(configuration.HttpHandlerOptions)
                .WithQosOptions(qosOptions)
                .WithDownstreamScheme(configuration.DownstreamScheme)
                .WithLoadBalancerOptions(configuration.LoadBalancerOptions)
                .Build();
        }

        private async Task<FileReRoute> GetDynamicRouteConfigurationAsync(string serviceName, IInternalConfiguration configuration)
        {
            var configurationProvider = _factory.Get(configuration);
            if (string.IsNullOrWhiteSpace(configuration?.DynamicReRouteConfiguration?.Host) ||
                string.IsNullOrWhiteSpace(configuration?.DynamicReRouteConfiguration?.Port))
            {
                _logger.LogWarning($"Host/port defined for dynamic configuration store is empty. Cannot reach the store.");
                return await Task.FromResult(new FileReRoute());
            }

            if (configurationProvider != null)
            {
                return await configurationProvider.BuildRouteConfigurationAsync(configuration.DynamicReRouteConfiguration.Host, configuration.DynamicReRouteConfiguration.Port, serviceName);
            }

            return await Task.FromResult(new FileReRoute());
        }

        private string CreateDownstreamPathForKeys(string serviceName, string downstreamPath)
        {
            return $"/{serviceName}{downstreamPath}";
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
            if(upstreamUrlPath.IndexOf('/', 1) == -1)
            {
                return "/";
            }

            return upstreamUrlPath
                .Substring(upstreamUrlPath.IndexOf('/', 1));
        }

        private static string GetServiceName(string upstreamUrlPath)
        {
            if(upstreamUrlPath.IndexOf('/', 1) == -1)
            {
                return upstreamUrlPath
                    .Substring(1);
            }

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
