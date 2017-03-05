using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class ReRoute
    {
        public ReRoute(PathTemplate downstreamPathTemplate, 
            PathTemplate upstreamTemplate, 
            HttpMethod upstreamHttpMethod, 
            string upstreamTemplatePattern, 
            bool isAuthenticated, 
            AuthenticationOptions authenticationOptions, 
            List<ClaimToThing> configurationHeaderExtractorProperties, 
            List<ClaimToThing> claimsToClaims, 
            Dictionary<string, string> routeClaimsRequirement, 
            bool isAuthorised, 
            List<ClaimToThing> claimsToQueries, 
            string requestIdKey, 
            bool isCached, 
            CacheOptions fileCacheOptions, 
            string downstreamScheme, 
            string loadBalancer, 
            string downstreamHost, 
            int downstreamPort, 
            string reRouteKey, 
            ServiceProviderConfiguration serviceProviderConfiguraion,
            bool isQos,
            QoSOptions qos,
            bool enableRateLimit,
            RateLimitOptions ratelimitOptions)
        {
            ReRouteKey = reRouteKey;
            ServiceProviderConfiguraion = serviceProviderConfiguraion;
            LoadBalancer = loadBalancer;
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
            DownstreamPathTemplate = downstreamPathTemplate;
            UpstreamPathTemplate = upstreamTemplate;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            IsAuthenticated = isAuthenticated;
            AuthenticationOptions = authenticationOptions;
            RouteClaimsRequirement = routeClaimsRequirement;
            IsAuthorised = isAuthorised;
            RequestIdKey = requestIdKey;
            IsCached = isCached;
            FileCacheOptions = fileCacheOptions;
            ClaimsToQueries = claimsToQueries
                ?? new List<ClaimToThing>();
            ClaimsToClaims = claimsToClaims 
                ?? new List<ClaimToThing>();
            ClaimsToHeaders = configurationHeaderExtractorProperties 
                ?? new List<ClaimToThing>();
                DownstreamScheme = downstreamScheme;
            IsQos = isQos;
            QosOptions = qos;
            EnableEndpointRateLimiting = enableRateLimit;
            RateLimitOptions = ratelimitOptions;
        }

        public string ReRouteKey {get;private set;}
        public PathTemplate DownstreamPathTemplate { get; private set; }
        public PathTemplate UpstreamPathTemplate { get; private set; }
        public string UpstreamTemplatePattern { get; private set; }
        public HttpMethod UpstreamHttpMethod { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsAuthorised { get; private set; }
        public AuthenticationOptions AuthenticationOptions { get; private set; }
        public List<ClaimToThing> ClaimsToQueries { get; private set; }
        public List<ClaimToThing> ClaimsToHeaders { get; private set; }
        public List<ClaimToThing> ClaimsToClaims { get; private set; }
        public Dictionary<string, string> RouteClaimsRequirement { get; private set; }
        public string RequestIdKey { get; private set; }
        public bool IsCached { get; private set; }
        public CacheOptions FileCacheOptions { get; private set; }
        public string DownstreamScheme {get;private set;}
        public bool IsQos { get; private set; }
        public QoSOptions QosOptions { get; private set; }
        public string LoadBalancer {get;private set;}
        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }
        public ServiceProviderConfiguration ServiceProviderConfiguraion { get; private set; }
        public bool EnableEndpointRateLimiting { get; private set; }
        public RateLimitOptions RateLimitOptions { get; private set; }
    }
}