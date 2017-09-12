using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Values;
using System.Linq;

namespace Ocelot.Configuration.Builder
{
    public class ReRouteBuilder
    {
        private AuthenticationOptions _authenticationOptions;
        private string _loadBalancerKey;
        private string _downstreamPathTemplate;
        private string _upstreamTemplate;
        private string _upstreamTemplatePattern;
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
        private string _downstreamHost;
        private int _downstreamPort;
        private string _loadBalancer;
        private ServiceProviderConfiguration _serviceProviderConfiguraion;
        private bool _useQos;
        private QoSOptions _qosOptions;
        public bool _enableRateLimiting;
        public RateLimitOptions _rateLimitOptions;

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

        public ReRouteBuilder WithUpstreamTemplatePattern(string input)
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

        public ReRouteBuilder WithDownstreamPort(int port)
        {
            _downstreamPort = port;
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

        public ReRouteBuilder WithLoadBalancerKey(string loadBalancerKey)
        {
            _loadBalancerKey = loadBalancerKey;
            return this;
        }

        public ReRouteBuilder WithServiceProviderConfiguraion(ServiceProviderConfiguration serviceProviderConfiguraion)
        {
            _serviceProviderConfiguraion = serviceProviderConfiguraion;
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

        /// <summary>
        /// 修改者:HY
        /// 新增转换方法: List<HttpMethod> 转  List<string>
        /// </summary>
        /// <param name="httpmethodls"></param>
        /// <returns></returns>
        public List<string> ConvertHttpMethodToString(List<HttpMethod> httpmethodls)
        {
            var HttpMethodLs = new List<string>();
            if (httpmethodls != null && httpmethodls.Count > 0)
            {
                foreach (var s in httpmethodls)
                {
                    if (HttpMethod.Equals(s.Method, HttpMethod.Delete))
                        HttpMethodLs.Add("Delete");
                    if (HttpMethod.Equals(s.Method, HttpMethod.Post))
                        HttpMethodLs.Add("Post");
                    if (HttpMethod.Equals(s.Method, HttpMethod.Get))
                        HttpMethodLs.Add("Get");
                    if (HttpMethod.Equals(s.Method, HttpMethod.Put))
                        HttpMethodLs.Add("Put");
                    if (HttpMethod.Equals(s.Method, HttpMethod.Head))
                        HttpMethodLs.Add("Head");
                }
            };
            return HttpMethodLs;
        }


        public ReRoute Build()
        {

            return new ReRoute(
                _downstreamPathTemplate,
                _upstreamTemplate,
               ConvertHttpMethodToString(_upstreamHttpMethod),
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
                _downstreamHost,
                _downstreamPort,
                _loadBalancerKey,
                _serviceProviderConfiguraion,
                _useQos,
                _qosOptions,
                _enableRateLimiting,
                _rateLimitOptions);
        }
    }
}
