using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Validator;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using Ocelot.Utilities;

namespace Ocelot.Configuration.Creator
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class FileOcelotConfigurationCreator : IOcelotConfigurationCreator
    {
        private readonly IOptions<FileConfiguration> _options;
        private readonly IConfigurationValidator _configurationValidator;
        private readonly ILogger<FileOcelotConfigurationCreator> _logger;
        private readonly ILoadBalancerFactory _loadBalanceFactory;
        private readonly ILoadBalancerHouse _loadBalancerHouse;
        private readonly IQoSProviderFactory _qoSProviderFactory;
        private readonly IQosProviderHouse _qosProviderHouse;
        private readonly IClaimsToThingCreator _claimsToThingCreator;
        private readonly IAuthenticationOptionsCreator _authOptionsCreator;
        private IUpstreamTemplatePatternCreator _upstreamTemplatePatternCreator;
        private IRequestIdKeyCreator _requestIdKeyCreator;
        private IServiceProviderConfigurationCreator _serviceProviderConfigCreator;
        private IQoSOptionsCreator _qosOptionsCreator;
        private IReRouteOptionsCreator _fileReRouteOptionsCreator;
        private IRateLimitOptionsCreator _rateLimitOptionsCreator;

        public FileOcelotConfigurationCreator(
            IOptions<FileConfiguration> options, 
            IConfigurationValidator configurationValidator, 
            ILogger<FileOcelotConfigurationCreator> logger,
            ILoadBalancerFactory loadBalancerFactory,
            ILoadBalancerHouse loadBalancerHouse, 
            IQoSProviderFactory qoSProviderFactory, 
            IQosProviderHouse qosProviderHouse,
            IClaimsToThingCreator claimsToThingCreator,
            IAuthenticationOptionsCreator authOptionsCreator,
            IUpstreamTemplatePatternCreator upstreamTemplatePatternCreator,
            IRequestIdKeyCreator requestIdKeyCreator,
            IServiceProviderConfigurationCreator serviceProviderConfigCreator,
            IQoSOptionsCreator qosOptionsCreator,
            IReRouteOptionsCreator fileReRouteOptionsCreator,
            IRateLimitOptionsCreator rateLimitOptionsCreator
            )
        {
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
            _requestIdKeyCreator = requestIdKeyCreator;
            _upstreamTemplatePatternCreator = upstreamTemplatePatternCreator;
            _authOptionsCreator = authOptionsCreator;
            _loadBalanceFactory = loadBalancerFactory;
            _loadBalancerHouse = loadBalancerHouse;
            _qoSProviderFactory = qoSProviderFactory;
            _qosProviderHouse = qosProviderHouse;
            _options = options;
            _configurationValidator = configurationValidator;
            _logger = logger;
            _claimsToThingCreator = claimsToThingCreator;
            _serviceProviderConfigCreator = serviceProviderConfigCreator;
            _qosOptionsCreator = qosOptionsCreator;
            _fileReRouteOptionsCreator = fileReRouteOptionsCreator;
        }

        public async Task<Response<IOcelotConfiguration>> Create()
        {     
            var config = await SetUpConfiguration(_options.Value);

            return new OkResponse<IOcelotConfiguration>(config);
        }

        public async Task<Response<IOcelotConfiguration>> Create(FileConfiguration fileConfiguration)
        {     
            var config = await SetUpConfiguration(fileConfiguration);

            return new OkResponse<IOcelotConfiguration>(config);
        }

        private async Task<IOcelotConfiguration> SetUpConfiguration(FileConfiguration fileConfiguration)
        {
            var response = _configurationValidator.IsValid(fileConfiguration);

            if (response.Data.IsError)
            {
                var errorBuilder = new StringBuilder();

                foreach (var error in response.Errors)
                {
                    errorBuilder.AppendLine(error.Message);
                }

                throw new Exception($"Unable to start Ocelot..configuration, errors were {errorBuilder}");
            }

            var reRoutes = new List<ReRoute>();

            foreach (var reRoute in fileConfiguration.ReRoutes)
            {
                var ocelotReRoute = await SetUpReRoute(reRoute, fileConfiguration.GlobalConfiguration);
                reRoutes.Add(ocelotReRoute);
            }
            
            return new OcelotConfiguration(reRoutes, fileConfiguration.GlobalConfiguration.AdministrationPath);
        }

        private async Task<ReRoute> SetUpReRoute(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var fileReRouteOptions = _fileReRouteOptionsCreator.Create(fileReRoute);

            var requestIdKey = _requestIdKeyCreator.Create(fileReRoute, globalConfiguration);

            var reRouteKey = CreateReRouteKey(fileReRoute);

            var upstreamTemplatePattern = _upstreamTemplatePatternCreator.Create(fileReRoute);

            var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(fileReRoute, globalConfiguration);

            var authOptionsForRoute = _authOptionsCreator.Create(fileReRoute);

            var claimsToHeaders = _claimsToThingCreator.Create(fileReRoute.AddHeadersToRequest);

            var claimsToClaims = _claimsToThingCreator.Create(fileReRoute.AddClaimsToRequest);

            var claimsToQueries = _claimsToThingCreator.Create(fileReRoute.AddQueriesToRequest);

            var qosOptions = _qosOptionsCreator.Create(fileReRoute);

            var rateLimitOption = _rateLimitOptionsCreator.Create(fileReRoute, globalConfiguration, fileReRouteOptions.EnableRateLimiting);

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
                .WithCacheOptions(new CacheOptions(fileReRoute.FileCacheOptions.TtlSeconds))
                .WithDownstreamScheme(fileReRoute.DownstreamScheme)
                .WithLoadBalancer(fileReRoute.LoadBalancer)
                .WithDownstreamHost(fileReRoute.DownstreamHost)
                .WithDownstreamPort(fileReRoute.DownstreamPort)
                .WithLoadBalancerKey(reRouteKey)
                .WithServiceProviderConfiguraion(serviceProviderConfiguration)
                .WithIsQos(fileReRouteOptions.IsQos)
                .WithQosOptions(qosOptions)
                .WithEnableRateLimiting(fileReRouteOptions.EnableRateLimiting)
                .WithRateLimitOptions(rateLimitOption)
                .Build();

            await SetupLoadBalancer(reRoute);
            SetupQosProvider(reRoute);
            return reRoute;
        }

        private string CreateReRouteKey(FileReRoute fileReRoute)
        {
            //note - not sure if this is the correct key, but this is probably the only unique key i can think of given my poor brain
            var loadBalancerKey = $"{fileReRoute.UpstreamPathTemplate}{fileReRoute.UpstreamHttpMethod}";
            return loadBalancerKey;
        }

        private async Task SetupLoadBalancer(ReRoute reRoute)
        {
            var loadBalancer = await _loadBalanceFactory.Get(reRoute);
            _loadBalancerHouse.Add(reRoute.ReRouteKey, loadBalancer);
        }

        private void SetupQosProvider(ReRoute reRoute)
        {
            var loadBalancer = _qoSProviderFactory.Get(reRoute);
            _qosProviderHouse.Add(reRoute.ReRouteKey, loadBalancer);
        }
    }
}