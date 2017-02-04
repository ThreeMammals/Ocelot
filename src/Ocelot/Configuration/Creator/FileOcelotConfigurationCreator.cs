using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        /// <summary>
        /// This method is meant to be tempoary to convert a config to an ocelot config...probably wont keep this but we will see
        /// will need a refactor at some point as its crap
        /// </summary>
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
            var globalRequestIdConfiguration = !string.IsNullOrEmpty(globalConfiguration?.RequestIdKey);

            var upstreamTemplate = BuildUpstreamTemplate(fileReRoute);

            var isAuthenticated = !string.IsNullOrEmpty(fileReRoute.AuthenticationOptions?.Provider);

            var isAuthorised = fileReRoute.RouteClaimsRequirement?.Count > 0;

            var isCached = fileReRoute.FileCacheOptions.TtlSeconds > 0;

            var requestIdKey = globalRequestIdConfiguration
                ? globalConfiguration.RequestIdKey
                : fileReRoute.RequestIdKey;

            var useServiceDiscovery = !string.IsNullOrEmpty(fileReRoute.ServiceName)
                && !string.IsNullOrEmpty(globalConfiguration?.ServiceDiscoveryProvider?.Provider);

            //note - not sure if this is the correct key, but this is probably the only unique key i can think of given my poor brain
            var loadBalancerKey = $"{fileReRoute.UpstreamTemplate}{fileReRoute.UpstreamHttpMethod}";

            ReRoute reRoute;

            var serviceProviderPort = globalConfiguration?.ServiceDiscoveryProvider?.Port ?? 0;

            var serviceProviderConfiguration = new ServiceProviderConfiguraion(fileReRoute.ServiceName,
                fileReRoute.DownstreamHost, fileReRoute.DownstreamPort, useServiceDiscovery,
                globalConfiguration?.ServiceDiscoveryProvider?.Provider, globalConfiguration?.ServiceDiscoveryProvider?.Host,
                serviceProviderPort);

            if (isAuthenticated)
            {
                var authOptionsForRoute = new AuthenticationOptions(fileReRoute.AuthenticationOptions.Provider,
                    fileReRoute.AuthenticationOptions.ProviderRootUrl, fileReRoute.AuthenticationOptions.ScopeName,
                    fileReRoute.AuthenticationOptions.RequireHttps, fileReRoute.AuthenticationOptions.AdditionalScopes,
                    fileReRoute.AuthenticationOptions.ScopeSecret);

                var claimsToHeaders = GetAddThingsToRequest(fileReRoute.AddHeadersToRequest);
                var claimsToClaims = GetAddThingsToRequest(fileReRoute.AddClaimsToRequest);
                var claimsToQueries = GetAddThingsToRequest(fileReRoute.AddQueriesToRequest);

                reRoute = new ReRoute(new DownstreamPathTemplate(fileReRoute.DownstreamPathTemplate),
                    fileReRoute.UpstreamTemplate,
                    fileReRoute.UpstreamHttpMethod, upstreamTemplate, isAuthenticated,
                    authOptionsForRoute, claimsToHeaders, claimsToClaims,
                    fileReRoute.RouteClaimsRequirement, isAuthorised, claimsToQueries,
                    requestIdKey, isCached, new CacheOptions(fileReRoute.FileCacheOptions.TtlSeconds)
                    , fileReRoute.DownstreamScheme,
                    fileReRoute.LoadBalancer, fileReRoute.DownstreamHost, fileReRoute.DownstreamPort, loadBalancerKey,
                    serviceProviderConfiguration);
            }
            else
            {
                reRoute = new ReRoute(new DownstreamPathTemplate(fileReRoute.DownstreamPathTemplate),
                    fileReRoute.UpstreamTemplate,
                    fileReRoute.UpstreamHttpMethod, upstreamTemplate, isAuthenticated,
                    null, new List<ClaimToThing>(), new List<ClaimToThing>(),
                    fileReRoute.RouteClaimsRequirement, isAuthorised, new List<ClaimToThing>(),
                    requestIdKey, isCached, new CacheOptions(fileReRoute.FileCacheOptions.TtlSeconds),
                    fileReRoute.DownstreamScheme,
                    fileReRoute.LoadBalancer, fileReRoute.DownstreamHost, fileReRoute.DownstreamPort, loadBalancerKey,
                    serviceProviderConfiguration);
            }

            var loadBalancer = await _loadBalanceFactory.Get(reRoute);
            _loadBalancerHouse.Add(reRoute.LoadBalancerKey, loadBalancer);
            return reRoute;
        }

        private string BuildUpstreamTemplate(FileReRoute reRoute)
        {
            var upstreamTemplate = reRoute.UpstreamTemplate;

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

        private List<ClaimToThing> GetAddThingsToRequest(Dictionary<string,string> thingBeingAdded)
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