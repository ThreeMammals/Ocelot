using System;
using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class ReRoute
    {
        public ReRoute(DownstreamPathTemplate downstreamPathTemplate, string upstreamTemplate, string upstreamHttpMethod, string upstreamTemplatePattern, 
            bool isAuthenticated, AuthenticationOptions authenticationOptions, List<ClaimToThing> configurationHeaderExtractorProperties, 
            List<ClaimToThing> claimsToClaims, Dictionary<string, string> routeClaimsRequirement, bool isAuthorised, List<ClaimToThing> claimsToQueries, 
            string requestIdKey, bool isCached, CacheOptions fileCacheOptions, string serviceName, bool useServiceDiscovery,
            string serviceDiscoveryProvider, string serviceDiscoveryAddress, Func<HostAndPort> downstreamHostAndPort, string downstreamScheme)
        {
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
                ServiceName = serviceName;
                UseServiceDiscovery = useServiceDiscovery;
                ServiceDiscoveryProvider = serviceDiscoveryProvider;
                ServiceDiscoveryAddress = serviceDiscoveryAddress;
                DownstreamHostAndPort = downstreamHostAndPort;
                DownstreamScheme = downstreamScheme;
        }

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
        public string ServiceName { get; private set;}
        public bool UseServiceDiscovery { get; private set;}
        public string ServiceDiscoveryProvider { get; private set;}
        public string ServiceDiscoveryAddress { get; private set;}
        public Func<HostAndPort> DownstreamHostAndPort {get;private set;}
        public string DownstreamScheme {get;private set;}
    }
}