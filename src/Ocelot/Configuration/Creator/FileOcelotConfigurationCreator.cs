using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Validator;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Utilities;
using Ocelot.Values;

namespace Ocelot.Configuration.Creator
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class FileOcelotConfigurationCreator : IOcelotConfigurationCreator
    {
        private readonly IOptions<FileConfiguration> _options;
        private readonly IConfigurationValidator _configurationValidator;
        private const string RegExMatchEverything = ".*";
        private const string RegExMatchEndString = "$";
        private const string RegExIgnoreCase = "(?i)";

        private readonly IClaimToThingConfigurationParser _claimToThingConfigurationParser;
        private readonly ILogger<FileOcelotConfigurationCreator> _logger;
        private readonly ILoadBalancerFactory _loadBalanceFactory;
        private readonly ILoadBalancerHouse _loadBalancerHouse;

        public FileOcelotConfigurationCreator(
            IOptions<FileConfiguration> options, 
            IConfigurationValidator configurationValidator, 
            IClaimToThingConfigurationParser claimToThingConfigurationParser, 
            ILogger<FileOcelotConfigurationCreator> logger,
            ILoadBalancerFactory loadBalancerFactory,
            ILoadBalancerHouse loadBalancerHouse)
        {
            _loadBalanceFactory = loadBalancerFactory;
            _loadBalancerHouse = loadBalancerHouse;
            _options = options;
            _configurationValidator = configurationValidator;
            _claimToThingConfigurationParser = claimToThingConfigurationParser;
            _logger = logger;
        }

        public async Task<Response<IOcelotConfiguration>> Create()
        {     
            var config = await SetUpConfiguration();

            return new OkResponse<IOcelotConfiguration>(config);
        }

        private async Task<IOcelotConfiguration> SetUpConfiguration()
        {
            var response = _configurationValidator.IsValid(_options.Value);

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

            foreach (var reRoute in _options.Value.ReRoutes)
            {
                var ocelotReRoute = await SetUpReRoute(reRoute, _options.Value.GlobalConfiguration);
                reRoutes.Add(ocelotReRoute);
            }
            
            return new OcelotConfiguration(reRoutes);
        }

        private async Task<ReRoute> SetUpReRoute(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var isAuthenticated = IsAuthenticated(fileReRoute);

            var isAuthorised = IsAuthenticated(fileReRoute);

            var isCached = IsCached(fileReRoute);

            var requestIdKey = BuildRequestId(fileReRoute, globalConfiguration);

            var loadBalancerKey = BuildLoadBalancerKey(fileReRoute);

            var upstreamTemplatePattern = BuildUpstreamTemplate(fileReRoute);

            var serviceProviderConfiguration = BuildServiceProviderConfiguration(fileReRoute, globalConfiguration);

            var authOptionsForRoute = BuildAuthenticationOptions(fileReRoute);

            var claimsToHeaders = BuildAddThingsToRequest(fileReRoute.AddHeadersToRequest);

            var claimsToClaims = BuildAddThingsToRequest(fileReRoute.AddClaimsToRequest);

            var claimsToQueries = BuildAddThingsToRequest(fileReRoute.AddQueriesToRequest);

            var reRoute = new ReRouteBuilder()
                .WithDownstreamPathTemplate(fileReRoute.DownstreamPathTemplate)
                .WithUpstreamPathTemplate(fileReRoute.UpstreamPathTemplate)
                .WithUpstreamHttpMethod(fileReRoute.UpstreamHttpMethod)
                .WithUpstreamTemplatePattern(upstreamTemplatePattern)
                .WithIsAuthenticated(isAuthenticated)
                .WithAuthenticationOptions(authOptionsForRoute)
                .WithClaimsToHeaders(claimsToHeaders)
                .WithClaimsToClaims(claimsToClaims)
                .WithRouteClaimsRequirement(fileReRoute.RouteClaimsRequirement)
                .WithIsAuthorised(isAuthorised)
                .WithClaimsToQueries(claimsToQueries)
                .WithRequestIdKey(requestIdKey)
                .WithIsCached(isCached)
                .WithCacheOptions(new CacheOptions(fileReRoute.FileCacheOptions.TtlSeconds))
                .WithDownstreamScheme(fileReRoute.DownstreamScheme)
                .WithLoadBalancer(fileReRoute.LoadBalancer)
                .WithDownstreamHost(fileReRoute.DownstreamHost)
                .WithDownstreamPort(fileReRoute.DownstreamPort)
                .WithLoadBalancerKey(loadBalancerKey)
                .WithServiceProviderConfiguraion(serviceProviderConfiguration)
                .Build();   

            await SetupLoadBalancer(reRoute);
            return reRoute;
        }

        private bool IsAuthenticated(FileReRoute fileReRoute)
        {
            return !string.IsNullOrEmpty(fileReRoute.AuthenticationOptions?.Provider);
        }

        private bool IsAuthorised(FileReRoute fileReRoute)
        {
            return fileReRoute.RouteClaimsRequirement?.Count > 0;
        }

        private bool IsCached(FileReRoute fileReRoute)
        {
            return fileReRoute.FileCacheOptions.TtlSeconds > 0;
        }

        private string BuildRequestId(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var globalRequestIdConfiguration = !string.IsNullOrEmpty(globalConfiguration?.RequestIdKey);

             var requestIdKey = globalRequestIdConfiguration
                ? globalConfiguration.RequestIdKey
                : fileReRoute.RequestIdKey;

                return requestIdKey;
        }

        private string BuildLoadBalancerKey(FileReRoute fileReRoute)
        {
            //note - not sure if this is the correct key, but this is probably the only unique key i can think of given my poor brain
            var loadBalancerKey = $"{fileReRoute.UpstreamPathTemplate}{fileReRoute.UpstreamHttpMethod}";
            return loadBalancerKey;
        }

        private AuthenticationOptions BuildAuthenticationOptions(FileReRoute fileReRoute)
        {
            return new AuthenticationOptionsBuilder()
                                        .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                                        .WithProviderRootUrl(fileReRoute.AuthenticationOptions?.ProviderRootUrl)
                                        .WithScopeName(fileReRoute.AuthenticationOptions?.ScopeName)
                                        .WithRequireHttps(fileReRoute.AuthenticationOptions.RequireHttps)
                                        .WithAdditionalScopes(fileReRoute.AuthenticationOptions?.AdditionalScopes)
                                        .WithScopeSecret(fileReRoute.AuthenticationOptions?.ScopeSecret)
                                        .Build();
        }

        private async Task SetupLoadBalancer(ReRoute reRoute)
        {
            var loadBalancer = await _loadBalanceFactory.Get(reRoute);
            _loadBalancerHouse.Add(reRoute.LoadBalancerKey, loadBalancer);
        }

        private ServiceProviderConfiguraion BuildServiceProviderConfiguration(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var useServiceDiscovery = !string.IsNullOrEmpty(fileReRoute.ServiceName)
                && !string.IsNullOrEmpty(globalConfiguration?.ServiceDiscoveryProvider?.Provider);

            var serviceProviderPort = globalConfiguration?.ServiceDiscoveryProvider?.Port ?? 0;

            return new ServiceProviderConfiguraionBuilder()
                    .WithServiceName(fileReRoute.ServiceName)
                    .WithDownstreamHost(fileReRoute.DownstreamHost)
                    .WithDownstreamPort(fileReRoute.DownstreamPort)
                    .WithUseServiceDiscovery(useServiceDiscovery)
                    .WithServiceDiscoveryProvider(globalConfiguration?.ServiceDiscoveryProvider?.Provider)
                    .WithServiceDiscoveryProviderHost(globalConfiguration?.ServiceDiscoveryProvider?.Host)
                    .WithServiceDiscoveryProviderPort(serviceProviderPort)
                    .Build();
        }

        private string BuildUpstreamTemplate(FileReRoute reRoute)
        {
            var upstreamTemplate = reRoute.UpstreamPathTemplate;

            upstreamTemplate = upstreamTemplate.SetLastCharacterAs('/');

            var placeholders = new List<string>();

            for (var i = 0; i < upstreamTemplate.Length; i++)
            {
                if (IsPlaceHolder(upstreamTemplate, i))
                {
                    var postitionOfPlaceHolderClosingBracket = upstreamTemplate.IndexOf('}', i);
                    var difference = postitionOfPlaceHolderClosingBracket - i + 1;
                    var variableName = upstreamTemplate.Substring(i, difference);
                    placeholders.Add(variableName);
                }
            }

            foreach (var placeholder in placeholders)
            {
                upstreamTemplate = upstreamTemplate.Replace(placeholder, RegExMatchEverything);
            }

            var route = reRoute.ReRouteIsCaseSensitive 
                ? $"{upstreamTemplate}{RegExMatchEndString}" 
                : $"{RegExIgnoreCase}{upstreamTemplate}{RegExMatchEndString}";

            return route;
        }

        private List<ClaimToThing> BuildAddThingsToRequest(Dictionary<string,string> thingBeingAdded)
        {
            var claimsToTHings = new List<ClaimToThing>();

            foreach (var add in thingBeingAdded)
            {
                var claimToHeader = _claimToThingConfigurationParser.Extract(add.Key, add.Value);

                if (claimToHeader.IsError)
                {
                    _logger.LogCritical(new EventId(1, "Application Failed to start"),
                        $"Unable to extract configuration for key: {add.Key} and value: {add.Value} your configuration file is incorrect");

                    throw new Exception(claimToHeader.Errors[0].Message);
                }
                claimsToTHings.Add(claimToHeader.Data);
            }

            return claimsToTHings;
        }

        private bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }
    }
}