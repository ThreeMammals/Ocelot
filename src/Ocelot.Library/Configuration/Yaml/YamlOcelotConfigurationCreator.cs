using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Library.Configuration.Creator;
using Ocelot.Library.Configuration.Parser;
using Ocelot.Library.Configuration.Repository;
using Ocelot.Library.Errors;
using Ocelot.Library.Responses;

namespace Ocelot.Library.Configuration.Yaml
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class YamlOcelotConfigurationCreator : IOcelotConfigurationCreator
    {
        private readonly IOptions<YamlConfiguration> _options;
        private readonly IConfigurationValidator _configurationValidator;
        private const string RegExMatchEverything = ".*";
        private const string RegExMatchEndString = "$";
        private readonly IClaimToHeaderConfigurationParser _claimToHeaderConfigurationParser;
        private readonly ILogger<YamlOcelotConfigurationCreator> _logger;

        public YamlOcelotConfigurationCreator(
            IOptions<YamlConfiguration> options, 
            IConfigurationValidator configurationValidator, 
            IClaimToHeaderConfigurationParser claimToHeaderConfigurationParser, 
            ILogger<YamlOcelotConfigurationCreator> logger)
        {
            _options = options;
            _configurationValidator = configurationValidator;
            _claimToHeaderConfigurationParser = claimToHeaderConfigurationParser;
            _logger = logger;
        }

        public Response<IOcelotConfiguration> Create()
        {     
            var config = SetUpConfiguration();

            return new OkResponse<IOcelotConfiguration>(config);
        }

        /// <summary>
        /// This method is meant to be tempoary to convert a yaml config to an ocelot config...probably wont keep this but we will see
        /// will need a refactor at some point as its crap
        /// </summary>
        private IOcelotConfiguration SetUpConfiguration()
        {
            var response = _configurationValidator.IsValid(_options.Value);

            var reRoutes = new List<ReRoute>();

            if (!response.IsError && !response.Data.IsError)
            {

                foreach (var yamlReRoute in _options.Value.ReRoutes)
                {
                    var ocelotReRoute = SetUpReRoute(yamlReRoute);
                    reRoutes.Add(ocelotReRoute);
                }
            }

            return new OcelotConfiguration(reRoutes);
        }

        private ReRoute SetUpReRoute(YamlReRoute reRoute)
        {
            var upstreamTemplate = reRoute.UpstreamTemplate;

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

            upstreamTemplate = $"{upstreamTemplate}{RegExMatchEndString}";

            var isAuthenticated = !string.IsNullOrEmpty(reRoute.AuthenticationOptions?.Provider);

            if (isAuthenticated)
            {
                var authOptionsForRoute = new AuthenticationOptions(reRoute.AuthenticationOptions.Provider,
                    reRoute.AuthenticationOptions.ProviderRootUrl, reRoute.AuthenticationOptions.ScopeName,
                    reRoute.AuthenticationOptions.RequireHttps, reRoute.AuthenticationOptions.AdditionalScopes,
                    reRoute.AuthenticationOptions.ScopeSecret);

                var configHeaders = GetHeadersToAddToRequest(reRoute);

                return new ReRoute(reRoute.DownstreamTemplate, reRoute.UpstreamTemplate,
                    reRoute.UpstreamHttpMethod, upstreamTemplate, isAuthenticated,
                    authOptionsForRoute, configHeaders
                    );
            }

            return new ReRoute(reRoute.DownstreamTemplate, reRoute.UpstreamTemplate, reRoute.UpstreamHttpMethod,
                upstreamTemplate, isAuthenticated, null, new List<ClaimToHeader>());
        }

        private List<ClaimToHeader> GetHeadersToAddToRequest(YamlReRoute reRoute)
        {
            var configHeaders = new List<ClaimToHeader>();

            foreach (var add in reRoute.AddHeadersToRequest)
            {
                var configurationHeader = _claimToHeaderConfigurationParser.Extract(add.Key, add.Value);

                if (configurationHeader.IsError)
                {
                    _logger.LogCritical(new EventId(1, "Application Failed to start"),
                        $"Unable to extract configuration for key: {add.Key} and value: {add.Value} your configuration file is incorrect");

                    throw new Exception(configurationHeader.Errors[0].Message);
                }
                configHeaders.Add(configurationHeader.Data);
            }

            return configHeaders;
        }

        private bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }
    }
}