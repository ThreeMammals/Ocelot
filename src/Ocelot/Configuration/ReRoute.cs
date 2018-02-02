using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Configuration.Creator;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class ReRoute
    {
        public ReRoute(PathTemplate downstreamPathTemplate, 
            PathTemplate upstreamPathTemplate, 
            List<HttpMethod> upstreamHttpMethod, 
            UpstreamPathTemplate upstreamTemplatePattern, 
            bool isAuthenticated, 
            AuthenticationOptions authenticationOptions, 
            List<ClaimToThing> claimsToHeaders, 
            List<ClaimToThing> claimsToClaims, 
            Dictionary<string, string> routeClaimsRequirement, 
            bool isAuthorised, 
            List<ClaimToThing> claimsToQueries, 
            string requestIdKey, 
            bool isCached, 
            CacheOptions cacheOptions, 
            string downstreamScheme, 
            string loadBalancer, 
            string reRouteKey, 
            bool isQos,
            QoSOptions qosOptions,
            bool enableEndpointRateLimiting,
            RateLimitOptions ratelimitOptions,
            HttpHandlerOptions httpHandlerOptions,
            bool useServiceDiscovery,
            string serviceName,
            List<HeaderFindAndReplace> upstreamHeadersFindAndReplace,
            List<HeaderFindAndReplace> downstreamHeadersFindAndReplace,
            List<DownstreamHostAndPort> downstreamAddresses,
            string upstreamHost)
        {
            UpstreamHost = upstreamHost;
            DownstreamHeadersFindAndReplace = downstreamHeadersFindAndReplace ?? new List<HeaderFindAndReplace>();
            UpstreamHeadersFindAndReplace = upstreamHeadersFindAndReplace ?? new List<HeaderFindAndReplace>();
            ServiceName = serviceName;
            UseServiceDiscovery = useServiceDiscovery;
            ReRouteKey = reRouteKey;
            LoadBalancer = loadBalancer;
            DownstreamAddresses = downstreamAddresses ?? new List<DownstreamHostAndPort>();
            DownstreamPathTemplate = downstreamPathTemplate;
            UpstreamPathTemplate = upstreamPathTemplate;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            IsAuthenticated = isAuthenticated;
            AuthenticationOptions = authenticationOptions;
            RouteClaimsRequirement = routeClaimsRequirement;
            IsAuthorised = isAuthorised;
            RequestIdKey = requestIdKey;
            IsCached = isCached;
            CacheOptions = cacheOptions;
            ClaimsToQueries = claimsToQueries ?? new List<ClaimToThing>();
            ClaimsToClaims = claimsToClaims ?? new List<ClaimToThing>();
            ClaimsToHeaders = claimsToHeaders ?? new List<ClaimToThing>();
            DownstreamScheme = downstreamScheme;
            IsQos = isQos;
            QosOptionsOptions = qosOptions;
            EnableEndpointEndpointRateLimiting = enableEndpointRateLimiting;
            RateLimitOptions = ratelimitOptions;
            HttpHandlerOptions = httpHandlerOptions;
        }

        public string ReRouteKey {get;private set;}
        public PathTemplate DownstreamPathTemplate { get; private set; }
        public PathTemplate UpstreamPathTemplate { get; private set; }
        public UpstreamPathTemplate UpstreamTemplatePattern { get; private set; }
        public List<HttpMethod> UpstreamHttpMethod { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsAuthorised { get; private set; }
        public AuthenticationOptions AuthenticationOptions { get; private set; }
        public List<ClaimToThing> ClaimsToQueries { get; private set; }
        public List<ClaimToThing> ClaimsToHeaders { get; private set; }
        public List<ClaimToThing> ClaimsToClaims { get; private set; }
        public Dictionary<string, string> RouteClaimsRequirement { get; private set; }
        public string RequestIdKey { get; private set; }
        public bool IsCached { get; private set; }
        public CacheOptions CacheOptions { get; private set; }
        public string DownstreamScheme {get;private set;}
        public bool IsQos { get; private set; }
        public QoSOptions QosOptionsOptions { get; private set; }
        public string LoadBalancer {get;private set;}
        public bool EnableEndpointEndpointRateLimiting { get; private set; }
        public RateLimitOptions RateLimitOptions { get; private set; }
        public HttpHandlerOptions HttpHandlerOptions { get; private set; }
        public bool UseServiceDiscovery {get;private set;}
        public string ServiceName {get;private set;}
        public List<HeaderFindAndReplace> UpstreamHeadersFindAndReplace {get;private set;}
        public List<HeaderFindAndReplace> DownstreamHeadersFindAndReplace {get;private set;}
        public List<DownstreamHostAndPort> DownstreamAddresses {get;private set;}
        public string UpstreamHost { get; private set; }
    }
}
