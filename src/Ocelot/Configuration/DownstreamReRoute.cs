using System.Collections.Generic;
using Ocelot.Configuration.Creator;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class DownstreamReRoute
    {
        public DownstreamReRoute(
            string key,
            PathTemplate upstreamPathTemplate,
            List<HeaderFindAndReplace> upstreamHeadersFindAndReplace,
            List<HeaderFindAndReplace> downstreamHeadersFindAndReplace, 
            List<DownstreamHostAndPort> downstreamAddresses, 
            string serviceName, 
            HttpHandlerOptions httpHandlerOptions, 
            bool useServiceDiscovery, 
            bool enableEndpointEndpointRateLimiting, 
            bool isQos, 
            QoSOptions qosOptionsOptions, 
            string downstreamScheme, 
            string requestIdKey, 
            bool isCached, 
            CacheOptions cacheOptions, 
            string loadBalancer, 
            RateLimitOptions rateLimitOptions, 
            Dictionary<string, string> routeClaimsRequirement, 
            List<ClaimToThing> claimsToQueries, 
            List<ClaimToThing> claimsToHeaders, 
            List<ClaimToThing> claimsToClaims, 
            bool isAuthenticated, 
            bool isAuthorised, 
            AuthenticationOptions authenticationOptions, 
            PathTemplate downstreamPathTemplate, 
            string reRouteKey,
            List<string> delegatingHandlers,
            List<AddHeader> addHeadersToDownstream)
        {
            AddHeadersToDownstream = addHeadersToDownstream;
            DelegatingHandlers = delegatingHandlers;
            Key = key;
            UpstreamPathTemplate = upstreamPathTemplate;
            UpstreamHeadersFindAndReplace = upstreamHeadersFindAndReplace ?? new List<HeaderFindAndReplace>();
            DownstreamHeadersFindAndReplace = downstreamHeadersFindAndReplace ?? new List<HeaderFindAndReplace>();
            DownstreamAddresses = downstreamAddresses ?? new List<DownstreamHostAndPort>();
            ServiceName = serviceName;
            HttpHandlerOptions = httpHandlerOptions;
            UseServiceDiscovery = useServiceDiscovery;
            EnableEndpointEndpointRateLimiting = enableEndpointEndpointRateLimiting;
            IsQos = isQos;
            QosOptionsOptions = qosOptionsOptions;
            DownstreamScheme = downstreamScheme;
            RequestIdKey = requestIdKey;
            IsCached = isCached;
            CacheOptions = cacheOptions;
            LoadBalancer = loadBalancer;
            RateLimitOptions = rateLimitOptions;
            RouteClaimsRequirement = routeClaimsRequirement;
            ClaimsToQueries = claimsToQueries ?? new List<ClaimToThing>();
            ClaimsToHeaders = claimsToHeaders ?? new List<ClaimToThing>();
            ClaimsToClaims = claimsToClaims ?? new List<ClaimToThing>();
            IsAuthenticated = isAuthenticated;
            IsAuthorised = isAuthorised;
            AuthenticationOptions = authenticationOptions;
            DownstreamPathTemplate = downstreamPathTemplate;
            ReRouteKey = reRouteKey;
        }

        public string Key { get; private set; }
        public PathTemplate UpstreamPathTemplate { get;private set; }
        public List<HeaderFindAndReplace> UpstreamHeadersFindAndReplace {get;private set;}
        public List<HeaderFindAndReplace> DownstreamHeadersFindAndReplace { get; private set; }
        public List<DownstreamHostAndPort> DownstreamAddresses { get; private set; }
        public string ServiceName { get; private set; }
        public HttpHandlerOptions HttpHandlerOptions { get; private set; }
        public bool UseServiceDiscovery { get; private set; }
        public bool EnableEndpointEndpointRateLimiting { get; private set; }
        public bool IsQos { get; private set; }
        public QoSOptions QosOptionsOptions { get; private set; }
        public string DownstreamScheme { get; private set; }
        public string RequestIdKey { get; private set; }
        public bool IsCached { get; private set; }
        public CacheOptions CacheOptions { get; private set; }
        public string LoadBalancer { get; private set; }
        public RateLimitOptions RateLimitOptions { get; private set; }
        public Dictionary<string, string> RouteClaimsRequirement { get; private set; }
        public List<ClaimToThing> ClaimsToQueries { get; private set; }
        public List<ClaimToThing> ClaimsToHeaders { get; private set; }
        public List<ClaimToThing> ClaimsToClaims { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsAuthorised { get; private set; }
        public AuthenticationOptions AuthenticationOptions { get; private set; }
        public PathTemplate DownstreamPathTemplate { get; private set; }
        public string ReRouteKey { get; private set; }
        public List<string> DelegatingHandlers {get;private set;}
        public List<AddHeader> AddHeadersToDownstream {get;private set;}
    }
}
