using System;
using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.Configuration.Builder
{
    public class ReRouteBuilder
    {
        private string _loadBalancerKey;
        private string _downstreamPathTemplate;
        private string _upstreamTemplate;
        private string _upstreamTemplatePattern;
        private string _upstreamHttpMethod;
        private bool _isAuthenticated;
        private string _authenticationProvider;
        private string _authenticationProviderUrl;
        private string _scopeName;
        private List<string> _additionalScopes;
        private bool _requireHttps;
        private string _scopeSecret;
        private List<ClaimToThing> _configHeaderExtractorProperties;
        private List<ClaimToThing> _claimToClaims;
        private Dictionary<string, string> _routeClaimRequirement;
        private bool _isAuthorised;
        private List<ClaimToThing> _claimToQueries;
        private string _requestIdHeaderKey;
        private bool _isCached;
        private CacheOptions _fileCacheOptions;
        private bool _useServiceDiscovery;
        private string _serviceName;
        private string _serviceDiscoveryProvider;
        private string _serviceDiscoveryAddress;
        private string _downstreamScheme;
        private string _downstreamHost;
        private int _dsPort;
        private string _loadBalancer;
        private string _serviceProviderHost;
        private int _serviceProviderPort;

        public ReRouteBuilder()
        {
            _additionalScopes = new List<string>();
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

        public ReRouteBuilder WithDownstreamHost(string downstreamHost)
        {
            _downstreamHost = downstreamHost;
            return this;
        }

        public ReRouteBuilder WithServiceDiscoveryAddress(string serviceDiscoveryAddress)
        {
            _serviceDiscoveryAddress = serviceDiscoveryAddress;
            return this;
        }

        public ReRouteBuilder WithServiceDiscoveryProvider(string serviceDiscoveryProvider)
        {
            _serviceDiscoveryProvider = serviceDiscoveryProvider;
            return this;
        }

        public ReRouteBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public ReRouteBuilder WithUseServiceDiscovery(bool useServiceDiscovery)
        {
            _useServiceDiscovery = useServiceDiscovery;
            return this;
        }

        public ReRouteBuilder WithDownstreamPathTemplate(string input)
        {
            _downstreamPathTemplate = input;
            return this;
        }

        public ReRouteBuilder WithUpstreamTemplate(string input)
        {
            _upstreamTemplate = input;
            return this;
        }

        public ReRouteBuilder WithUpstreamTemplatePattern(string input)
        {
            _upstreamTemplatePattern = input;
            return this;
        }
        public ReRouteBuilder WithUpstreamHttpMethod(string input)
        {
            _upstreamHttpMethod = input;
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

        public ReRouteBuilder WithAuthenticationProvider(string input)
        {
            _authenticationProvider = input;
            return this;
        }

        public ReRouteBuilder WithAuthenticationProviderUrl(string input)
        {
            _authenticationProviderUrl = input;
            return this;
        }

        public ReRouteBuilder WithAuthenticationProviderScopeName(string input)
        {
            _scopeName = input;
            return this;
        }

        public ReRouteBuilder WithAuthenticationProviderAdditionalScopes(List<string> input)
        {
            _additionalScopes = input;
            return this;
        }

        public ReRouteBuilder WithRequireHttps(bool input)
        {
            _requireHttps = input;
            return this;
        }

        public ReRouteBuilder WithScopeSecret(string input)
        {
            _scopeSecret = input;
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

        public ReRouteBuilder WithDownstreamPort(int port)
        {
            _dsPort = port;
            return this;
        }

        public ReRouteBuilder WithLoadBalancerKey(string loadBalancerKey)
        {
            _loadBalancerKey = loadBalancerKey;
            return this;
        }

        public ReRouteBuilder WithServiceProviderHost(string serviceProviderHost)
        {
            _serviceProviderHost = serviceProviderHost;
            return this;
        }

        public ReRouteBuilder WithServiceProviderPort(int serviceProviderPort)
        {
            _serviceProviderPort = serviceProviderPort;
            return this;
        }

        public ReRoute Build()
        {
            return new ReRoute(new DownstreamPathTemplate(_downstreamPathTemplate), _upstreamTemplate, _upstreamHttpMethod, _upstreamTemplatePattern, 
                _isAuthenticated, new AuthenticationOptions(_authenticationProvider, _authenticationProviderUrl, _scopeName, 
                _requireHttps, _additionalScopes, _scopeSecret), _configHeaderExtractorProperties, _claimToClaims, _routeClaimRequirement, 
                _isAuthorised, _claimToQueries, _requestIdHeaderKey, _isCached, _fileCacheOptions, _downstreamScheme, _loadBalancer,
                _downstreamHost, _dsPort, _loadBalancerKey, new ServiceProviderConfiguraion(_serviceName, _downstreamHost, _dsPort, _useServiceDiscovery, _serviceDiscoveryProvider, _serviceProviderHost, _serviceProviderPort));
        }
    }
}
