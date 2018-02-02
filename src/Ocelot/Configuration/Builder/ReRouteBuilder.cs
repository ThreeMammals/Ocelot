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
        private AuthenticationOptions _authenticationOptions;
        private string _reRouteKey;
        private string _downstreamPathTemplate;
        private string _upstreamTemplate;
        private UpstreamPathTemplate _upstreamTemplatePattern;
        private List<HttpMethod> _upstreamHttpMethod;
        private bool _isAuthenticated;
        private List<ClaimToThing> _configHeaderExtractorProperties;
        private List<ClaimToThing> _claimToClaims;
        private Dictionary<string, string> _routeClaimRequirement;
        private bool _isAuthorised;
        private List<ClaimToThing> _claimToQueries;
        private string _requestIdHeaderKey;
        private bool _isCached;
        private CacheOptions _fileCacheOptions;
        private string _downstreamScheme;
        private string _loadBalancer;
        private bool _useQos;
        private QoSOptions _qosOptions;
        private HttpHandlerOptions _httpHandlerOptions;
        private bool _enableRateLimiting;
        private RateLimitOptions _rateLimitOptions;
        private bool _useServiceDiscovery;
        private string _serviceName;
        private List<HeaderFindAndReplace> _upstreamHeaderFindAndReplace;
        private List<HeaderFindAndReplace> _downstreamHeaderFindAndReplace;
        private readonly List<DownstreamHostAndPort> _downstreamAddresses;
        private string _upstreamHost;

        public ReRouteBuilder()
        {
            _downstreamAddresses = new List<DownstreamHostAndPort>();
        }

        public ReRouteBuilder WithDownstreamAddresses(List<DownstreamHostAndPort> downstreamAddresses)
        {
            _downstreamAddresses.AddRange(downstreamAddresses);
            return this;
        }

        public ReRouteBuilder WithUpstreamHost(string upstreamAddresses)
        {
            _upstreamHost = upstreamAddresses;
            return this;
        }

        public ReRouteBuilder WithLoadBalancer(string loadBalancer)
        {
          _loadBalancer = loadBalancer;
            return this;
        }

        public ReRouteBuilder WithDownstreamScheme(string downstreamScheme)
        {
            _downstreamScheme = downstreamScheme;
            return this;
        }

        public ReRouteBuilder WithDownstreamPathTemplate(string input)
        {
            _downstreamPathTemplate = input;
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

        public ReRouteBuilder WithIsAuthenticated(bool input)
        {
            _isAuthenticated = input;
            return this;
        }

        public ReRouteBuilder WithIsAuthorised(bool input)
        {
            _isAuthorised = input;
            return this;
        }

        public ReRouteBuilder WithRequestIdKey(string input)
        {
            _requestIdHeaderKey = input;
            return this;
        }

        public ReRouteBuilder WithClaimsToHeaders(List<ClaimToThing> input)
        {
            _configHeaderExtractorProperties = input;
            return this;
        }

        public ReRouteBuilder WithClaimsToClaims(List<ClaimToThing> input)
        {
            _claimToClaims = input;
            return this;
        }

        public ReRouteBuilder WithRouteClaimsRequirement(Dictionary<string, string> input)
        {
            _routeClaimRequirement = input;
            return this;
        }

        public ReRouteBuilder WithClaimsToQueries(List<ClaimToThing> input)
        {
            _claimToQueries = input;
            return this;
        }

        public ReRouteBuilder WithIsCached(bool input)
        {
            _isCached = input;
            return this;
        }

        public ReRouteBuilder WithCacheOptions(CacheOptions input)
        {
            _fileCacheOptions = input;
            return this;
        }

        public ReRouteBuilder WithIsQos(bool input)
        {
            _useQos = input;
            return this;
        }

        public ReRouteBuilder WithQosOptions(QoSOptions input)
        {
            _qosOptions = input;
            return this;
        }
       
        public ReRouteBuilder WithReRouteKey(string reRouteKey)
        {
            _reRouteKey = reRouteKey;
            return this;
        }

        public ReRouteBuilder WithAuthenticationOptions(AuthenticationOptions authenticationOptions)
        {
            _authenticationOptions = authenticationOptions;
            return this;
        }

        public ReRouteBuilder WithEnableRateLimiting(bool input)
        {
            _enableRateLimiting = input;
            return this;
        }

        public ReRouteBuilder WithRateLimitOptions(RateLimitOptions input)
        {
            _rateLimitOptions = input;
            return this;
        }

        public ReRouteBuilder WithHttpHandlerOptions(HttpHandlerOptions input)
        {
            _httpHandlerOptions = input;
            return this;
        }

        public ReRouteBuilder WithUseServiceDiscovery(bool useServiceDiscovery)
        {
            _useServiceDiscovery = useServiceDiscovery;
            return this;
        }

        public ReRouteBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public ReRouteBuilder WithUpstreamHeaderFindAndReplace(List<HeaderFindAndReplace> upstreamHeaderFindAndReplace)
        {
            _upstreamHeaderFindAndReplace = upstreamHeaderFindAndReplace;
            return this;
        }

        public ReRouteBuilder WithDownstreamHeaderFindAndReplace(List<HeaderFindAndReplace> downstreamHeaderFindAndReplace)
        {
            _downstreamHeaderFindAndReplace = downstreamHeaderFindAndReplace;
            return this;
        }


        public ReRoute Build()
        {
            return new ReRoute(
                new PathTemplate(_downstreamPathTemplate), 
                new PathTemplate(_upstreamTemplate), 
                _upstreamHttpMethod, 
                _upstreamTemplatePattern, 
                _isAuthenticated, 
                _authenticationOptions,
                _configHeaderExtractorProperties, 
                _claimToClaims, 
                _routeClaimRequirement, 
                _isAuthorised, 
                _claimToQueries, 
                _requestIdHeaderKey, 
                _isCached, 
                _fileCacheOptions, 
                _downstreamScheme, 
                _loadBalancer,
                _reRouteKey, 
                _useQos, 
                _qosOptions,
                _enableRateLimiting,
                _rateLimitOptions,
                _httpHandlerOptions,
                _useServiceDiscovery,
                _serviceName,
                _upstreamHeaderFindAndReplace,
                _downstreamHeaderFindAndReplace,
                _downstreamAddresses,
                _upstreamHost);
        }
    }
}
