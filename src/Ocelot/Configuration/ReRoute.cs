using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class ReRoute
    {
        public ReRoute(PathTemplate downstreamPathTemplate, 
            PathTemplate upstreamPathTemplate, 
            List<HttpMethod> upstreamHttpMethod, 
            string upstreamTemplatePattern, 
            bool isAuthenticated, 
            AuthenticationOptions authenticationOptions, 
            List<ClaimToThing> claimsToHeaders, 
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
            QoSOptions qosOptions,
            bool enableEndpointRateLimiting,
            RateLimitOptions ratelimitOptions)
        {
            ReRouteKey = reRouteKey;
            ServiceProviderConfiguraion = serviceProviderConfiguraion;
            LoadBalancer = loadBalancer;
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
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
            CacheOptions = fileCacheOptions;
            ClaimsToQueries = claimsToQueries
                ?? new List<ClaimToThing>();
            ClaimsToClaims = claimsToClaims 
                ?? new List<ClaimToThing>();
            ClaimsToHeaders = claimsToHeaders 
                ?? new List<ClaimToThing>();
            DownstreamScheme = downstreamScheme;
            IsQos = isQos;
            QosOptionsOptions = qosOptions;
            EnableEndpointEndpointRateLimiting = enableEndpointRateLimiting;
            RateLimitOptions = ratelimitOptions;
        }

        public string ReRouteKey {get;private set;}
        public PathTemplate DownstreamPathTemplate { get; private set; }
        public PathTemplate UpstreamPathTemplate { get; private set; }
        public string UpstreamTemplatePattern { get; private set; }
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
        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }
        public ServiceProviderConfiguration ServiceProviderConfiguraion { get; private set; }
        public bool EnableEndpointEndpointRateLimiting { get; private set; }
        public RateLimitOptions RateLimitOptions { get; private set; }
    }
}