using Ocelot.Configuration.Creator;
using Ocelot.Values;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Ocelot.Configuration.Builder
{
    public class DownstreamReRouteBuilder
    {
        private AuthenticationOptions _authenticationOptions;
        private string _loadBalancerKey;
        private string _downstreamPathTemplate;
        private UpstreamPathTemplate _upstreamTemplatePattern;
        private List<HttpMethod> _upstreamHttpMethod;
        private bool _isAuthenticated;
        private List<ClaimToThing> _claimsToHeaders;
        private List<ClaimToThing> _claimToClaims;
        private Dictionary<string, string> _routeClaimRequirement;
        private bool _isAuthorised;
        private List<ClaimToThing> _claimToQueries;
        private List<ClaimToThing> _claimToDownstreamPath;
        private string _requestIdHeaderKey;
        private bool _isCached;
        private CacheOptions _fileCacheOptions;
        private string _downstreamScheme;
        private LoadBalancerOptions _loadBalancerOptions;
        private QoSOptions _qosOptions;
        private HttpHandlerOptions _httpHandlerOptions;
        private bool _enableRateLimiting;
        private RateLimitOptions _rateLimitOptions;
        private bool _useServiceDiscovery;
        private string _serviceName;
        private string _serviceNamespace;
        private List<HeaderFindAndReplace> _upstreamHeaderFindAndReplace;
        private List<HeaderFindAndReplace> _downstreamHeaderFindAndReplace;
        private readonly List<DownstreamHostAndPort> _downstreamAddresses;
        private string _key;
        private List<string> _delegatingHandlers;
        private List<AddHeader> _addHeadersToDownstream;
        private List<AddHeader> _addHeadersToUpstream;
        private bool _dangerousAcceptAnyServerCertificateValidator;
        private SecurityOptions _securityOptions;

        public DownstreamReRouteBuilder()
        {
            _downstreamAddresses = new List<DownstreamHostAndPort>();
            _delegatingHandlers = new List<string>();
            _addHeadersToDownstream = new List<AddHeader>();
            _addHeadersToUpstream = new List<AddHeader>();
        }

        public DownstreamReRouteBuilder WithDownstreamAddresses(List<DownstreamHostAndPort> downstreamAddresses)
        {
            _downstreamAddresses.AddRange(downstreamAddresses);
            return this;
        }

        public DownstreamReRouteBuilder WithLoadBalancerOptions(LoadBalancerOptions loadBalancerOptions)
        {
            _loadBalancerOptions = loadBalancerOptions;
            return this;
        }

        public DownstreamReRouteBuilder WithDownstreamScheme(string downstreamScheme)
        {
            _downstreamScheme = downstreamScheme;
            return this;
        }

        public DownstreamReRouteBuilder WithDownstreamPathTemplate(string input)
        {
            _downstreamPathTemplate = input;
            return this;
        }

        public DownstreamReRouteBuilder WithUpstreamPathTemplate(UpstreamPathTemplate input)
        {
            _upstreamTemplatePattern = input;
            return this;
        }

        public DownstreamReRouteBuilder WithUpstreamHttpMethod(List<string> input)
        {
            _upstreamHttpMethod = (input.Count == 0) ? new List<HttpMethod>() : input.Select(x => new HttpMethod(x.Trim())).ToList();
            return this;
        }

        public DownstreamReRouteBuilder WithIsAuthenticated(bool input)
        {
            _isAuthenticated = input;
            return this;
        }

        public DownstreamReRouteBuilder WithIsAuthorised(bool input)
        {
            _isAuthorised = input;
            return this;
        }

        public DownstreamReRouteBuilder WithRequestIdKey(string input)
        {
            _requestIdHeaderKey = input;
            return this;
        }

        public DownstreamReRouteBuilder WithClaimsToHeaders(List<ClaimToThing> input)
        {
            _claimsToHeaders = input;
            return this;
        }

        public DownstreamReRouteBuilder WithClaimsToClaims(List<ClaimToThing> input)
        {
            _claimToClaims = input;
            return this;
        }

        public DownstreamReRouteBuilder WithRouteClaimsRequirement(Dictionary<string, string> input)
        {
            _routeClaimRequirement = input;
            return this;
        }

        public DownstreamReRouteBuilder WithClaimsToQueries(List<ClaimToThing> input)
        {
            _claimToQueries = input;
            return this;
        }

        public DownstreamReRouteBuilder WithClaimsToDownstreamPath(List<ClaimToThing> input)
        {
            _claimToDownstreamPath = input;
            return this;
        }

        public DownstreamReRouteBuilder WithIsCached(bool input)
        {
            _isCached = input;
            return this;
        }

        public DownstreamReRouteBuilder WithCacheOptions(CacheOptions input)
        {
            _fileCacheOptions = input;
            return this;
        }

        public DownstreamReRouteBuilder WithQosOptions(QoSOptions input)
        {
            _qosOptions = input;
            return this;
        }

        public DownstreamReRouteBuilder WithLoadBalancerKey(string loadBalancerKey)
        {
            _loadBalancerKey = loadBalancerKey;
            return this;
        }

        public DownstreamReRouteBuilder WithAuthenticationOptions(AuthenticationOptions authenticationOptions)
        {
            _authenticationOptions = authenticationOptions;
            return this;
        }

        public DownstreamReRouteBuilder WithEnableRateLimiting(bool input)
        {
            _enableRateLimiting = input;
            return this;
        }

        public DownstreamReRouteBuilder WithRateLimitOptions(RateLimitOptions input)
        {
            _rateLimitOptions = input;
            return this;
        }

        public DownstreamReRouteBuilder WithHttpHandlerOptions(HttpHandlerOptions input)
        {
            _httpHandlerOptions = input;
            return this;
        }

        public DownstreamReRouteBuilder WithUseServiceDiscovery(bool useServiceDiscovery)
        {
            _useServiceDiscovery = useServiceDiscovery;
            return this;
        }

        public DownstreamReRouteBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public DownstreamReRouteBuilder WithServiceNamespace(string serviceNamespace)
        {
            _serviceNamespace = serviceNamespace;
            return this;
        }

        public DownstreamReRouteBuilder WithUpstreamHeaderFindAndReplace(List<HeaderFindAndReplace> upstreamHeaderFindAndReplace)
        {
            _upstreamHeaderFindAndReplace = upstreamHeaderFindAndReplace;
            return this;
        }

        public DownstreamReRouteBuilder WithDownstreamHeaderFindAndReplace(List<HeaderFindAndReplace> downstreamHeaderFindAndReplace)
        {
            _downstreamHeaderFindAndReplace = downstreamHeaderFindAndReplace;
            return this;
        }

        public DownstreamReRouteBuilder WithKey(string key)
        {
            _key = key;
            return this;
        }

        public DownstreamReRouteBuilder WithDelegatingHandlers(List<string> delegatingHandlers)
        {
            _delegatingHandlers = delegatingHandlers;
            return this;
        }

        public DownstreamReRouteBuilder WithAddHeadersToDownstream(List<AddHeader> addHeadersToDownstream)
        {
            _addHeadersToDownstream = addHeadersToDownstream;
            return this;
        }

        public DownstreamReRouteBuilder WithAddHeadersToUpstream(List<AddHeader> addHeadersToUpstream)
        {
            _addHeadersToUpstream = addHeadersToUpstream;
            return this;
        }

        public DownstreamReRouteBuilder WithDangerousAcceptAnyServerCertificateValidator(bool dangerousAcceptAnyServerCertificateValidator)
        {
            _dangerousAcceptAnyServerCertificateValidator = dangerousAcceptAnyServerCertificateValidator;
            return this;
        }

        public DownstreamReRouteBuilder WithSecurityOptions(SecurityOptions securityOptions)
        {
            _securityOptions = securityOptions;
            return this;
        }

        public DownstreamReRoute Build()
        {
            return new DownstreamReRoute(
                _key,
                _upstreamTemplatePattern,
                _upstreamHeaderFindAndReplace,
                _downstreamHeaderFindAndReplace,
                _downstreamAddresses,
                _serviceName,
                _serviceNamespace,
                _httpHandlerOptions,
                _useServiceDiscovery,
                _enableRateLimiting,
                _qosOptions,
                _downstreamScheme,
                _requestIdHeaderKey,
                _isCached,
                _fileCacheOptions,
                _loadBalancerOptions,
                _rateLimitOptions,
                _routeClaimRequirement,
                _claimToQueries,
                _claimsToHeaders,
                _claimToClaims,
                _claimToDownstreamPath,
                _isAuthenticated,
                _isAuthorised,
                _authenticationOptions,
                new DownstreamPathTemplate(_downstreamPathTemplate),
                _loadBalancerKey,
                _delegatingHandlers,
                _addHeadersToDownstream,
                _addHeadersToUpstream,
                _dangerousAcceptAnyServerCertificateValidator,
                _securityOptions);
        }
    }
}
