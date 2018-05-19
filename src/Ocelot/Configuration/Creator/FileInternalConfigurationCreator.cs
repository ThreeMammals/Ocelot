using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ocelot.Cache;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Responses;

namespace Ocelot.Configuration.Creator
{
    using LoadBalancer.LoadBalancers;

    /// <summary>
    /// Register as singleton
    /// </summary>
    public class FileInternalConfigurationCreator : IInternalConfigurationCreator
    {
        private readonly IConfigurationValidator _configurationValidator;
        private readonly IOcelotLogger _logger;
        private readonly IClaimsToThingCreator _claimsToThingCreator;
        private readonly IAuthenticationOptionsCreator _authOptionsCreator;
        private readonly IUpstreamTemplatePatternCreator _upstreamTemplatePatternCreator;
        private readonly IRequestIdKeyCreator _requestIdKeyCreator;
        private readonly IServiceProviderConfigurationCreator _serviceProviderConfigCreator;
        private readonly IQoSOptionsCreator _qosOptionsCreator;
        private readonly IReRouteOptionsCreator _fileReRouteOptionsCreator;
        private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
        private readonly IRegionCreator _regionCreator;
        private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private readonly IAdministrationPath _adminPath;
        private readonly IHeaderFindAndReplaceCreator _headerFAndRCreator;
        private readonly IDownstreamAddressesCreator _downstreamAddressesCreator;

        public FileInternalConfigurationCreator(
            IConfigurationValidator configurationValidator,
            IOcelotLoggerFactory loggerFactory,
            IClaimsToThingCreator claimsToThingCreator,
            IAuthenticationOptionsCreator authOptionsCreator,
            IUpstreamTemplatePatternCreator upstreamTemplatePatternCreator,
            IRequestIdKeyCreator requestIdKeyCreator,
            IServiceProviderConfigurationCreator serviceProviderConfigCreator,
            IQoSOptionsCreator qosOptionsCreator,
            IReRouteOptionsCreator fileReRouteOptionsCreator,
            IRateLimitOptionsCreator rateLimitOptionsCreator,
            IRegionCreator regionCreator,
            IHttpHandlerOptionsCreator httpHandlerOptionsCreator,
            IAdministrationPath adminPath,
            IHeaderFindAndReplaceCreator headerFAndRCreator,
            IDownstreamAddressesCreator downstreamAddressesCreator
            )
        {
            _downstreamAddressesCreator = downstreamAddressesCreator;
            _headerFAndRCreator = headerFAndRCreator;
            _adminPath = adminPath;
            _regionCreator = regionCreator;
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
            _requestIdKeyCreator = requestIdKeyCreator;
            _upstreamTemplatePatternCreator = upstreamTemplatePatternCreator;
            _authOptionsCreator = authOptionsCreator;
            _configurationValidator = configurationValidator;
            _logger = loggerFactory.CreateLogger<FileInternalConfigurationCreator>();
            _claimsToThingCreator = claimsToThingCreator;
            _serviceProviderConfigCreator = serviceProviderConfigCreator;
            _qosOptionsCreator = qosOptionsCreator;
            _fileReRouteOptionsCreator = fileReRouteOptionsCreator;
            _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
        }
        
        public async Task<Response<IInternalConfiguration>> Create(FileConfiguration fileConfiguration)
        {     
            var config = await SetUpConfiguration(fileConfiguration);
            return config;
        }

        private async Task<Response<IInternalConfiguration>> SetUpConfiguration(FileConfiguration fileConfiguration)
        {
            var response = await _configurationValidator.IsValid(fileConfiguration);

            if (response.Data.IsError)
            {
                return new ErrorResponse<IInternalConfiguration>(response.Data.Errors);
            }

            var reRoutes = new List<ReRoute>();

            foreach (var reRoute in fileConfiguration.ReRoutes)
            {
                var downstreamReRoute = SetUpDownstreamReRoute(reRoute, fileConfiguration.GlobalConfiguration);

                var ocelotReRoute = SetUpReRoute(reRoute, downstreamReRoute);
                
                reRoutes.Add(ocelotReRoute);
            }

            foreach (var aggregate in fileConfiguration.Aggregates)
            {
                var ocelotReRoute = SetUpAggregateReRoute(reRoutes, aggregate, fileConfiguration.GlobalConfiguration);
                reRoutes.Add(ocelotReRoute);
            }

            var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(fileConfiguration.GlobalConfiguration);

            var lbOptions = CreateLoadBalancerOptions(fileConfiguration.GlobalConfiguration.LoadBalancerOptions);

            var qosOptions = _qosOptionsCreator.Create(fileConfiguration.GlobalConfiguration.QoSOptions);

            var httpHandlerOptions = _httpHandlerOptionsCreator.Create(fileConfiguration.GlobalConfiguration.HttpHandlerOptions);

            var config = new InternalConfiguration(reRoutes, 
                _adminPath.Path, 
                serviceProviderConfiguration, 
                fileConfiguration.GlobalConfiguration.RequestIdKey, 
                lbOptions, 
                fileConfiguration.GlobalConfiguration.DownstreamScheme,
                qosOptions,
                httpHandlerOptions
                );

            return new OkResponse<IInternalConfiguration>(config);
        }

        public ReRoute SetUpAggregateReRoute(List<ReRoute> reRoutes, FileAggregateReRoute aggregateReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var applicableReRoutes = reRoutes
                .SelectMany(x => x.DownstreamReRoute)
                .Where(r => aggregateReRoute.ReRouteKeys.Contains(r.Key))
                .ToList();

            if(applicableReRoutes.Count != aggregateReRoute.ReRouteKeys.Count)
            {
                //todo - log or throw or return error whatever?
            }

            //make another re route out of these
            var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(aggregateReRoute);

            var reRoute = new ReRouteBuilder()
                .WithUpstreamPathTemplate(aggregateReRoute.UpstreamPathTemplate)
                .WithUpstreamHttpMethod(aggregateReRoute.UpstreamHttpMethod)
                .WithUpstreamTemplatePattern(upstreamTemplatePattern)
                .WithDownstreamReRoutes(applicableReRoutes)
                .WithUpstreamHost(aggregateReRoute.UpstreamHost)
                .WithAggregator(aggregateReRoute.Aggregator)
                .Build();

            return reRoute;
        }

        private ReRoute SetUpReRoute(FileReRoute fileReRoute, DownstreamReRoute downstreamReRoutes)
        {
            var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileReRoute);

            var reRoute = new ReRouteBuilder()
                .WithUpstreamPathTemplate(fileReRoute.UpstreamPathTemplate)
                .WithUpstreamHttpMethod(fileReRoute.UpstreamHttpMethod)
                .WithUpstreamTemplatePattern(upstreamTemplatePattern)
                .WithDownstreamReRoute(downstreamReRoutes)
                .WithUpstreamHost(fileReRoute.UpstreamHost)
                .Build();

            return reRoute;
        }

         private DownstreamReRoute SetUpDownstreamReRoute(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var fileReRouteOptions = _fileReRouteOptionsCreator.Create(fileReRoute);

            var requestIdKey = _requestIdKeyCreator.Create(fileReRoute, globalConfiguration);

            var reRouteKey = CreateReRouteKey(fileReRoute);

            var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileReRoute);

            var authOptionsForRoute = _authOptionsCreator.Create(fileReRoute);

            var claimsToHeaders = _claimsToThingCreator.Create(fileReRoute.AddHeadersToRequest);

            var claimsToClaims = _claimsToThingCreator.Create(fileReRoute.AddClaimsToRequest);

            var claimsToQueries = _claimsToThingCreator.Create(fileReRoute.AddQueriesToRequest);

            var qosOptions = _qosOptionsCreator.Create(fileReRoute.QoSOptions, fileReRoute.UpstreamPathTemplate, fileReRoute.UpstreamHttpMethod.ToArray());

            var rateLimitOption = _rateLimitOptionsCreator.Create(fileReRoute, globalConfiguration, fileReRouteOptions.EnableRateLimiting);

            var region = _regionCreator.Create(fileReRoute);

            var httpHandlerOptions = _httpHandlerOptionsCreator.Create(fileReRoute.HttpHandlerOptions);

            var hAndRs = _headerFAndRCreator.Create(fileReRoute);

            var downstreamAddresses = _downstreamAddressesCreator.Create(fileReRoute);

            var lbOptions = CreateLoadBalancerOptions(fileReRoute.LoadBalancerOptions);

            var reRoute = new DownstreamReRouteBuilder()
                .WithKey(fileReRoute.Key)
                .WithDownstreamPathTemplate(fileReRoute.DownstreamPathTemplate)
                .WithUpstreamPathTemplate(fileReRoute.UpstreamPathTemplate)
                .WithUpstreamHttpMethod(fileReRoute.UpstreamHttpMethod)
                .WithUpstreamTemplatePattern(upstreamTemplatePattern)
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
                .WithUseServiceDiscovery(fileReRoute.UseServiceDiscovery)
                .WithUpstreamHeaderFindAndReplace(hAndRs.Upstream)
                .WithDownstreamHeaderFindAndReplace(hAndRs.Downstream)
                .WithUpstreamHost(fileReRoute.UpstreamHost)
                .WithDelegatingHandlers(fileReRoute.DelegatingHandlers)
                .WithAddHeadersToDownstream(hAndRs.AddHeadersToDownstream)
                .WithAddHeadersToUpstream(hAndRs.AddHeadersToUpstream)
                .WithDangerousAcceptAnyServerCertificateValidator(fileReRoute.DangerousAcceptAnyServerCertificateValidator)
                .Build();

            return reRoute;
        }

        private LoadBalancerOptions CreateLoadBalancerOptions(FileLoadBalancerOptions options)
        {
            return new LoadBalancerOptionsBuilder()
                .WithType(options.Type)
                .WithKey(options.Key)
                .WithExpiryInMs(options.Expiry)
                .Build();
        }

        private string CreateReRouteKey(FileReRoute fileReRoute)
        {
            if (!string.IsNullOrEmpty(fileReRoute.LoadBalancerOptions.Type) && !string.IsNullOrEmpty(fileReRoute.LoadBalancerOptions.Key) && fileReRoute.LoadBalancerOptions.Type == nameof(CookieStickySessions))
            {
                return $"{nameof(CookieStickySessions)}:{fileReRoute.LoadBalancerOptions.Key}";
            }

            return $"{fileReRoute.UpstreamPathTemplate}|{string.Join(",", fileReRoute.UpstreamHttpMethod)}";
        }
    }
}
