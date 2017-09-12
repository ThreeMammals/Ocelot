using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Values;

namespace Ocelot.Configuration
{
    public class ReRoute
    {
        /// <summary>
        /// 修改人:HY
        /// 此处修改:  PathTemplate downstreamPathTemplate 改为 string downstreamPathTemplate
        ///           PathTemplate upstreamPathTemplate 改为 string upstreamPathTemplate
        ///           List<HtthMethod> upstreamHttpMethod 改为 List<string> upstreamHttpMethod
        ///           
        ///          改动原因:原模型不对,导致反序列化失败 Ocelot.Configuration.Repository.ConsulOcelotConfigurationRepository中53行序列化保错
        /// </summary>
        /// <param name="httpmethodls"></param>
        /// <returns></returns>
        public ReRoute(string downstreamPathTemplate,
           string upstreamPathTemplate,
            List<string> upstreamHttpMethod,
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
            DownstreamPathTemplate = new PathTemplate(downstreamPathTemplate);
            UpstreamPathTemplate = new PathTemplate(upstreamPathTemplate);
            UpstreamHttpMethod = ConvertStringToHttpMethod(upstreamHttpMethod);
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
        /// <summary>
        /// 修改人:HY
        /// 功能:对象转换  List<string> =>List<HttpMethod>
        /// </summary>
        /// <param name="httpmethodls"></param>
        /// <returns></returns>
        public static List<HttpMethod> ConvertStringToHttpMethod(List<string> httpmethodls)
        {
            var HttpMethodLs = new List<HttpMethod>();
            if (httpmethodls != null && httpmethodls.Count > 0)
            {
                foreach (var s in httpmethodls)
                {
                    switch (s.ToUpper())
                    {
                        case "DELETE":
                            HttpMethodLs.Add(HttpMethod.Delete);
                            break;
                        case "POST":
                            HttpMethodLs.Add(HttpMethod.Post);
                            break;
                        case "GET":
                            HttpMethodLs.Add(HttpMethod.Get);
                            break;
                        case "PUT":
                            HttpMethodLs.Add(HttpMethod.Put);
                            break;
                        case "HEAD":
                            HttpMethodLs.Add(HttpMethod.Head);
                            break;
                        default: break;
                    }
                };
            }
            return HttpMethodLs;
        }
        public string ReRouteKey { get; private set; }
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
        public string DownstreamScheme { get; private set; }
        public bool IsQos { get; private set; }
        public QoSOptions QosOptionsOptions { get; private set; }
        public string LoadBalancer { get; private set; }
        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }
        public ServiceProviderConfiguration ServiceProviderConfiguraion { get; private set; }
        public bool EnableEndpointEndpointRateLimiting { get; private set; }
        public RateLimitOptions RateLimitOptions { get; private set; }
    }
}