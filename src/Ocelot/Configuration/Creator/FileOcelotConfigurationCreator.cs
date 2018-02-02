using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Cache;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Validator;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Requester.QoS;
using Ocelot.Responses;

namespace Ocelot.Configuration.Creator
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class FileOcelotConfigurationCreator : IOcelotConfigurationCreator
    {
        private readonly IOptions<FileConfiguration> _options;
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


        public FileOcelotConfigurationCreator(
            IOptions<FileConfiguration> options, 
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
            _options = options;
            _configurationValidator = configurationValidator;
            _logger = loggerFactory.CreateLogger<FileOcelotConfigurationCreator>();
            _claimsToThingCreator = claimsToThingCreator;
            _serviceProviderConfigCreator = serviceProviderConfigCreator;
            _qosOptionsCreator = qosOptionsCreator;
            _fileReRouteOptionsCreator = fileReRouteOptionsCreator;
            _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
        }
        
        public async Task<Response<IOcelotConfiguration>> Create(FileConfiguration fileConfiguration)
        {     
            var config = await SetUpConfiguration(fileConfiguration);
            return config;
        }

        private async Task<Response<IOcelotConfiguration>> SetUpConfiguration(FileConfiguration fileConfiguration)
        {
            var response = await _configurationValidator.IsValid(fileConfiguration);

            if (response.Data.IsError)
            {
                return new ErrorResponse<IOcelotConfiguration>(response.Data.Errors);
            }

            var reRoutes = new List<ReRoute>();

            foreach (var reRoute in fileConfiguration.ReRoutes)
            {
                var ocelotReRoute = SetUpReRoute(reRoute, fileConfiguration.GlobalConfiguration);
                reRoutes.Add(ocelotReRoute);
            }

            var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(fileConfiguration.GlobalConfiguration);
            
            var config = new OcelotConfiguration(reRoutes, _adminPath.Path, serviceProviderConfiguration, fileConfiguration.GlobalConfiguration.RequestIdKey);

            return new OkResponse<IOcelotConfiguration>(config);
        }

        private ReRoute SetUpReRoute(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var fileReRouteOptions = _fileReRouteOptionsCreator.Create(fileReRoute);

            var requestIdKey = _requestIdKeyCreator.Create(fileReRoute, globalConfiguration);

            var reRouteKey = CreateReRouteKey(fileReRoute);

            var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileReRoute);

            var authOptionsForRoute = _authOptionsCreator.Create(fileReRoute);

            var claimsToHeaders = _claimsToThingCreator.Create(fileReRoute.AddHeadersToRequest);

            var claimsToClaims = _claimsToThingCreator.Create(fileReRoute.AddClaimsToRequest);

            var claimsToQueries = _claimsToThingCreator.Create(fileReRoute.AddQueriesToRequest);

            var qosOptions = _qosOptionsCreator.Create(fileReRoute);

            var rateLimitOption = _rateLimitOptionsCreator.Create(fileReRoute, globalConfiguration, fileReRouteOptions.EnableRateLimiting);

            var region = _regionCreator.Create(fileReRoute);

            var httpHandlerOptions = _httpHandlerOptionsCreator.Create(fileReRoute);

            var hAndRs = _headerFAndRCreator.Create(fileReRoute);

            var downstreamAddresses = _downstreamAddressesCreator.Create(fileReRoute);

            var reRoute = new ReRouteBuilder()
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
                .WithLoadBalancer(fileReRoute.LoadBalancer)
                .WithDownstreamAddresses(downstreamAddresses)
                .WithReRouteKey(reRouteKey)
                .WithIsQos(fileReRouteOptions.IsQos)
                .WithQosOptions(qosOptions)
                .WithEnableRateLimiting(fileReRouteOptions.EnableRateLimiting)
                .WithRateLimitOptions(rateLimitOption)
                .WithHttpHandlerOptions(httpHandlerOptions)
                .WithServiceName(fileReRoute.ServiceName)
                .WithUseServiceDiscovery(fileReRoute.UseServiceDiscovery)
                .WithUpstreamHeaderFindAndReplace(hAndRs.Upstream)
                .WithDownstreamHeaderFindAndReplace(hAndRs.Downstream)
                .WithUpstreamHost(fileReRoute.UpstreamHost)
                .Build();

            return reRoute;
        }

        private string CreateReRouteKey(FileReRoute fileReRoute)
        {
            //note - not sure if this is the correct key, but this is probably the only unique key i can think of given my poor brain
            var loadBalancerKey = $"{fileReRoute.UpstreamPathTemplate}|{string.Join(",", fileReRoute.UpstreamHttpMethod)}";
            return loadBalancerKey;
        }
    }
}
