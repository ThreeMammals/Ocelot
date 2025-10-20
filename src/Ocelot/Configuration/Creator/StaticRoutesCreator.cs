using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class StaticRoutesCreator : IRoutesCreator
{
    private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;
    private readonly IClaimsToThingCreator _claimsToThingCreator;
    private readonly IAuthenticationOptionsCreator _authOptionsCreator;
    private readonly IUpstreamTemplatePatternCreator _upstreamTemplatePatternCreator;
    private readonly IUpstreamHeaderTemplatePatternCreator _upstreamHeaderTemplatePatternCreator;
    private readonly IRequestIdKeyCreator _requestIdKeyCreator;
    private readonly IQoSOptionsCreator _qosOptionsCreator;
    private readonly IRouteOptionsCreator _fileRouteOptionsCreator;
    private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
    private readonly ICacheOptionsCreator _cacheOptionsCreator;
    private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
    private readonly IHeaderFindAndReplaceCreator _headerFAndRCreator;
    private readonly IDownstreamAddressesCreator _downstreamAddressesCreator;
    private readonly IRouteKeyCreator _routeKeyCreator;
    private readonly ISecurityOptionsCreator _securityOptionsCreator;
    private readonly IVersionCreator _versionCreator;
    private readonly IVersionPolicyCreator _versionPolicyCreator;
    private readonly IMetadataCreator _metadataCreator;

    public StaticRoutesCreator(
        IClaimsToThingCreator claimsToThingCreator,
        IAuthenticationOptionsCreator authOptionsCreator,
        IUpstreamTemplatePatternCreator upstreamTemplatePatternCreator,
        IRequestIdKeyCreator requestIdKeyCreator,
        IQoSOptionsCreator qosOptionsCreator,
        IRouteOptionsCreator fileRouteOptionsCreator,
        IRateLimitOptionsCreator rateLimitOptionsCreator,
        ICacheOptionsCreator cacheOptionsCreator,
        IHttpHandlerOptionsCreator httpHandlerOptionsCreator,
        IHeaderFindAndReplaceCreator headerFAndRCreator,
        IDownstreamAddressesCreator downstreamAddressesCreator,
        ILoadBalancerOptionsCreator loadBalancerOptionsCreator,
        IRouteKeyCreator routeKeyCreator,
        ISecurityOptionsCreator securityOptionsCreator,
        IVersionCreator versionCreator,
        IVersionPolicyCreator versionPolicyCreator,
        IUpstreamHeaderTemplatePatternCreator upstreamHeaderTemplatePatternCreator,
        IMetadataCreator metadataCreator)
    {
        _routeKeyCreator = routeKeyCreator;
        _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
        _downstreamAddressesCreator = downstreamAddressesCreator;
        _headerFAndRCreator = headerFAndRCreator;
        _cacheOptionsCreator = cacheOptionsCreator;
        _rateLimitOptionsCreator = rateLimitOptionsCreator;
        _requestIdKeyCreator = requestIdKeyCreator;
        _upstreamTemplatePatternCreator = upstreamTemplatePatternCreator;
        _authOptionsCreator = authOptionsCreator;
        _claimsToThingCreator = claimsToThingCreator;
        _qosOptionsCreator = qosOptionsCreator;
        _fileRouteOptionsCreator = fileRouteOptionsCreator;
        _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
        _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
        _securityOptionsCreator = securityOptionsCreator;
        _versionCreator = versionCreator;
        _versionPolicyCreator = versionPolicyCreator;
        _upstreamHeaderTemplatePatternCreator = upstreamHeaderTemplatePatternCreator;
        _metadataCreator = metadataCreator;
    }

    public IReadOnlyList<Route> Create(FileConfiguration fileConfiguration)
    {
        Route CreateRoute(FileRoute route)
            => SetUpRoute(route, SetUpDownstreamRoute(route, fileConfiguration.GlobalConfiguration));
        return fileConfiguration.Routes
            .Select(CreateRoute)
            .ToArray();
    }

    public virtual int CreateTimeout(FileRoute route, FileGlobalConfiguration global)
    {
        int def = DownstreamRoute.DefaultTimeoutSeconds;
        return route.Timeout.Positive(def) ?? global.Timeout.Positive(def) ?? def;
    }

    private DownstreamRoute SetUpDownstreamRoute(FileRoute fileRoute, FileGlobalConfiguration globalConfiguration)
    {
        var fileRouteOptions = _fileRouteOptionsCreator.Create(fileRoute, globalConfiguration); // TODO Refactor this overhead service by moving options to native creators

        var requestIdKey = _requestIdKeyCreator.Create(fileRoute, globalConfiguration);

        var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileRoute);

        var authOptionsForRoute = _authOptionsCreator.Create(fileRoute, globalConfiguration);

        var claimsToHeaders = _claimsToThingCreator.Create(fileRoute.AddHeadersToRequest);

        var claimsToClaims = _claimsToThingCreator.Create(fileRoute.AddClaimsToRequest);

        var claimsToQueries = _claimsToThingCreator.Create(fileRoute.AddQueriesToRequest);

        var claimsToDownstreamPath = _claimsToThingCreator.Create(fileRoute.ChangeDownstreamPathTemplate);

        var qosOptions = _qosOptionsCreator.Create(fileRoute, globalConfiguration);

        var rateLimitOption = _rateLimitOptionsCreator.Create(fileRoute, globalConfiguration);

        var httpHandlerOptions = _httpHandlerOptionsCreator.Create(fileRoute.HttpHandlerOptions);

        var hAndRs = _headerFAndRCreator.Create(fileRoute, globalConfiguration);

        var downstreamAddresses = _downstreamAddressesCreator.Create(fileRoute);

        var lbOptions = _loadBalancerOptionsCreator.Create(fileRoute, globalConfiguration);
        var lbKey = _routeKeyCreator.Create(fileRoute, lbOptions);

        var securityOptions = _securityOptionsCreator.Create(fileRoute.SecurityOptions, globalConfiguration);

        var downstreamHttpVersion = _versionCreator.Create(fileRoute.DownstreamHttpVersion);

        var downstreamHttpVersionPolicy = _versionPolicyCreator.Create(fileRoute.DownstreamHttpVersionPolicy);

        var cacheOptions = _cacheOptionsCreator.Create(fileRoute.FileCacheOptions ?? fileRoute.CacheOptions, globalConfiguration, fileRoute.UpstreamPathTemplate, fileRoute.UpstreamHttpMethod);

        var metadata = _metadataCreator.Create(fileRoute.Metadata, globalConfiguration);

        var route = new DownstreamRouteBuilder()
            .WithKey(fileRoute.Key)
            .WithDownstreamPathTemplate(fileRoute.DownstreamPathTemplate)
            .WithUpstreamHttpMethod(fileRoute.UpstreamHttpMethod.ToList())
            .WithUpstreamPathTemplate(upstreamTemplatePattern)
            .WithIsAuthenticated(fileRouteOptions.IsAuthenticated)
            .WithAuthenticationOptions(authOptionsForRoute)
            .WithClaimsToHeaders(claimsToHeaders)
            .WithClaimsToClaims(claimsToClaims)
            .WithRouteClaimsRequirement(fileRoute.RouteClaimsRequirement)
            .WithIsAuthorized(fileRouteOptions.IsAuthorized)
            .WithClaimsToQueries(claimsToQueries)
            .WithClaimsToDownstreamPath(claimsToDownstreamPath)
            .WithRequestIdKey(requestIdKey)
            .WithIsCached(fileRouteOptions.IsCached)
            .WithCacheOptions(cacheOptions)
            .WithDownstreamScheme(fileRoute.DownstreamScheme)
            .WithLoadBalancerKey(lbKey)
            .WithLoadBalancerOptions(lbOptions)
            .WithDownstreamAddresses(downstreamAddresses)
            .WithQosOptions(qosOptions)
            .WithRateLimitOptions(rateLimitOption)
            .WithHttpHandlerOptions(httpHandlerOptions)
            .WithServiceName(fileRoute.ServiceName)
            .WithServiceNamespace(fileRoute.ServiceNamespace)
            .WithUseServiceDiscovery(fileRouteOptions.UseServiceDiscovery)
            .WithUpstreamHeaderFindAndReplace(hAndRs.Upstream)
            .WithDownstreamHeaderFindAndReplace(hAndRs.Downstream)
            .WithDelegatingHandlers(fileRoute.DelegatingHandlers)
            .WithAddHeadersToDownstream(hAndRs.AddHeadersToDownstream)
            .WithAddHeadersToUpstream(hAndRs.AddHeadersToUpstream)
            .WithDangerousAcceptAnyServerCertificateValidator(fileRoute.DangerousAcceptAnyServerCertificateValidator)
            .WithSecurityOptions(securityOptions)
            .WithDownstreamHttpVersion(downstreamHttpVersion)
            .WithDownstreamHttpVersionPolicy(downstreamHttpVersionPolicy)
            .WithDownStreamHttpMethod(fileRoute.DownstreamHttpMethod)
            .WithMetadata(metadata)
            .WithTimeout(CreateTimeout(fileRoute, globalConfiguration))
            .Build();
        return route;
    }

    private Route SetUpRoute(FileRoute fileRoute, DownstreamRoute downstreamRoute)
    {
        var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileRoute); // TODO It should be downstreamRoute.UpstreamPathTemplate
        var upstreamHeaderTemplates = _upstreamHeaderTemplatePatternCreator.Create(fileRoute); // TODO It should be downstreamRoute.UpstreamHeaders
        var upstreamHttpMethods = fileRoute.UpstreamHttpMethod.ToHttpMethods();
        return new Route(downstreamRoute)
        {
            UpstreamHeaderTemplates = upstreamHeaderTemplates, // downstreamRoute.UpstreamHeaders
            UpstreamHost = fileRoute.UpstreamHost,
            UpstreamHttpMethod = upstreamHttpMethods,
            UpstreamTemplatePattern = upstreamTemplatePattern,
        };
    }
}
