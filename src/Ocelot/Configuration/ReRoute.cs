using System;
using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class ReRoute
    {
        public ReRoute(DownstreamPathTemplate downstreamPathTemplate, 
            string upstreamTemplate, string upstreamHttpMethod, 
            string upstreamTemplatePattern, 
            bool isAuthenticated, AuthenticationOptions authenticationOptions, 
            List<ClaimToThing> configurationHeaderExtractorProperties, 
            List<ClaimToThing> claimsToClaims, 
            Dictionary<string, string> routeClaimsRequirement, bool isAuthorised, 
            List<ClaimToThing> claimsToQueries, 
            string requestIdKey, bool isCached, CacheOptions fileCacheOptions, 
            string downstreamScheme, string loadBalancer, string downstreamHost, 
            int downstreamPort, string loadBalancerKey, ServiceProviderConfiguraion serviceProviderConfiguraion)
        {
            LoadBalancerKey = loadBalancerKey;
            ServiceProviderConfiguraion = serviceProviderConfiguraion;
            LoadBalancer = loadBalancer;
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
            DownstreamPathTemplate = downstreamPathTemplate;
            UpstreamTemplate = upstreamTemplate;
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
        }

        public string LoadBalancerKey {get;private set;}
        public DownstreamPathTemplate DownstreamPathTemplate { get; private set; }
        public string UpstreamTemplate { get; private set; }
        public string UpstreamTemplatePattern { get; private set; }
        public string UpstreamHttpMethod { get; private set; }
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
        public string LoadBalancer {get;private set;}
        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }
        public ServiceProviderConfiguraion ServiceProviderConfiguraion { get; private set; }
    }
}