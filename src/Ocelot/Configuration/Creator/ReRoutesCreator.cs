namespace Ocelot.Configuration.Creator
{
    using System.Collections.Generic;
    using System.Linq;
    using Cache;
    using Builder;
    using File;

    public class ReRoutesCreator : IReRoutesCreator
    {
        private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;
        private readonly IClaimsToThingCreator _claimsToThingCreator;
        private readonly IAuthenticationOptionsCreator _authOptionsCreator;
        private readonly IUpstreamTemplatePatternCreator _upstreamTemplatePatternCreator;
        private readonly IRequestIdKeyCreator _requestIdKeyCreator;
        private readonly IQoSOptionsCreator _qosOptionsCreator;
        private readonly IReRouteOptionsCreator _fileReRouteOptionsCreator;
        private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
        private readonly IRegionCreator _regionCreator;
        private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private readonly IHeaderFindAndReplaceCreator _headerFAndRCreator;
        private readonly IDownstreamAddressesCreator _downstreamAddressesCreator;
        private readonly IReRouteKeyCreator _reRouteKeyCreator;
        private readonly ISecurityOptionsCreator _securityOptionsCreator;

        public ReRoutesCreator(
            IClaimsToThingCreator claimsToThingCreator,
            IAuthenticationOptionsCreator authOptionsCreator,
            IUpstreamTemplatePatternCreator upstreamTemplatePatternCreator,
            IRequestIdKeyCreator requestIdKeyCreator,
            IQoSOptionsCreator qosOptionsCreator,
            IReRouteOptionsCreator fileReRouteOptionsCreator,
            IRateLimitOptionsCreator rateLimitOptionsCreator,
            IRegionCreator regionCreator,
            IHttpHandlerOptionsCreator httpHandlerOptionsCreator,
            IHeaderFindAndReplaceCreator headerFAndRCreator,
            IDownstreamAddressesCreator downstreamAddressesCreator,
            ILoadBalancerOptionsCreator loadBalancerOptionsCreator,
            IReRouteKeyCreator reRouteKeyCreator,
            ISecurityOptionsCreator securityOptionsCreator
            )
        {
            _reRouteKeyCreator = reRouteKeyCreator;
            _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
            _downstreamAddressesCreator = downstreamAddressesCreator;
            _headerFAndRCreator = headerFAndRCreator;
            _regionCreator = regionCreator;
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
            _requestIdKeyCreator = requestIdKeyCreator;
            _upstreamTemplatePatternCreator = upstreamTemplatePatternCreator;
            _authOptionsCreator = authOptionsCreator;
            _claimsToThingCreator = claimsToThingCreator;
            _qosOptionsCreator = qosOptionsCreator;
            _fileReRouteOptionsCreator = fileReRouteOptionsCreator;
            _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
            _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
            _securityOptionsCreator = securityOptionsCreator;
        }

        public List<ReRoute> Create(FileConfiguration fileConfiguration)
        {
            return fileConfiguration.ReRoutes
                .Select(reRoute =>
                {
                    var downstreamReRoute = SetUpDownstreamReRoute(reRoute, fileConfiguration.GlobalConfiguration);
                    return SetUpReRoute(reRoute, downstreamReRoute);
                })
                .ToList();
        }

        private DownstreamReRoute SetUpDownstreamReRoute(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var fileReRouteOptions = _fileReRouteOptionsCreator.Create(fileReRoute);

            var requestIdKey = _requestIdKeyCreator.Create(fileReRoute, globalConfiguration);

            var reRouteKey = _reRouteKeyCreator.Create(fileReRoute);

            var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileReRoute);

            var authOptionsForRoute = _authOptionsCreator.Create(fileReRoute);

            var claimsToHeaders = _claimsToThingCreator.Create(fileReRoute.AddHeadersToRequest);

            var claimsToClaims = _claimsToThingCreator.Create(fileReRoute.AddClaimsToRequest);

            var claimsToQueries = _claimsToThingCreator.Create(fileReRoute.AddQueriesToRequest);

            var qosOptions = _qosOptionsCreator.Create(fileReRoute.QoSOptions, fileReRoute.UpstreamPathTemplate, fileReRoute.UpstreamHttpMethod);

            var rateLimitOption = _rateLimitOptionsCreator.Create(fileReRoute.RateLimitOptions, globalConfiguration);

            var region = _regionCreator.Create(fileReRoute);

            var httpHandlerOptions = _httpHandlerOptionsCreator.Create(fileReRoute.HttpHandlerOptions);

            var hAndRs = _headerFAndRCreator.Create(fileReRoute);

            var downstreamAddresses = _downstreamAddressesCreator.Create(fileReRoute);

            var lbOptions = _loadBalancerOptionsCreator.Create(fileReRoute.LoadBalancerOptions);

            var securityOptions = _securityOptionsCreator.Create(fileReRoute.SecurityOptions);

            var reRoute = new DownstreamReRouteBuilder()
                .WithKey(fileReRoute.Key)
                .WithDownstreamPathTemplate(fileReRoute.DownstreamPathTemplate)
                .WithUpstreamHttpMethod(fileReRoute.UpstreamHttpMethod)
                .WithUpstreamPathTemplate(upstreamTemplatePattern)
                .WithIsAuthenticated(fileReRouteOptions.IsAuthenticated)
                .WithAuthenticationOptions(authOptionsForRoute)
                .WithClaimsToHeaders(claimsToHeaders)
                .WithClaimsToClaims(claimsToClaims)
                .WithRouteClaimsRequirement(fileReRoute.RouteClaimsRequirement)
                .WithIsAuthorised(fileReRouteOptions.IsAuthorised)
                .WithClaimsToQueries(claimsToQueries)
                .WithRequestIdKey(requestIdKey)
                .WithIsCached(fileReRouteOptions.IsCached)
                .WithCacheOptions(new CacheOptions(fileReRoute.FileCacheOptions.TtlSeconds, region))
                .WithDownstreamScheme(fileReRoute.DownstreamScheme)
                .WithLoadBalancerOptions(lbOptions)
                .WithDownstreamAddresses(downstreamAddresses)
                .WithLoadBalancerKey(reRouteKey)
                .WithQosOptions(qosOptions)
                .WithEnableRateLimiting(fileReRouteOptions.EnableRateLimiting)
                .WithRateLimitOptions(rateLimitOption)
                .WithHttpHandlerOptions(httpHandlerOptions)
                .WithServiceName(fileReRoute.ServiceName)
                .WithUseServiceDiscovery(fileReRouteOptions.UseServiceDiscovery)
                .WithUpstreamHeaderFindAndReplace(hAndRs.Upstream)
                .WithDownstreamHeaderFindAndReplace(hAndRs.Downstream)
                .WithDelegatingHandlers(fileReRoute.DelegatingHandlers)
                .WithAddHeadersToDownstream(hAndRs.AddHeadersToDownstream)
                .WithAddHeadersToUpstream(hAndRs.AddHeadersToUpstream)
                .WithDangerousAcceptAnyServerCertificateValidator(fileReRoute.DangerousAcceptAnyServerCertificateValidator)
                .WithSecurityOptions(securityOptions)
                .Build();

            return reRoute;
        }

        private ReRoute SetUpReRoute(FileReRoute fileReRoute, DownstreamReRoute downstreamReRoutes)
        {
            var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileReRoute);

            var reRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod(fileReRoute.UpstreamHttpMethod)
                .WithUpstreamPathTemplate(upstreamTemplatePattern)
                .WithDownstreamReRoute(downstreamReRoutes)
                .WithUpstreamHost(fileReRoute.UpstreamHost)
                .Build();

            return reRoute;
        }
    }
}
