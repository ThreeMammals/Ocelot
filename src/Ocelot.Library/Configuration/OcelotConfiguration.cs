namespace Ocelot.Library.Configuration
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;
    using Yaml;

    public class OcelotConfiguration : IOcelotConfiguration
    {
        private readonly IOptions<YamlConfiguration> _options;
        private readonly IConfigurationValidator _configurationValidator;
        private readonly List<ReRoute> _reRoutes;
        private const string RegExMatchEverything = ".*";
        private const string RegExMatchEndString = "$";

        public OcelotConfiguration(IOptions<YamlConfiguration> options, IConfigurationValidator configurationValidator)
        {
            _options = options;
            _configurationValidator = configurationValidator;
            _reRoutes = new List<ReRoute>();
            SetUpConfiguration();
        }

        /// <summary>
        /// This method is meant to be tempoary to convert a yaml config to an ocelot config...probably wont keep this but we will see
        /// will need a refactor at some point as its crap
        /// </summary>
        private void SetUpConfiguration()
        {
            var response = _configurationValidator.IsValid(_options.Value);

            if (!response.IsError && !response.Data.IsError)
            {
                foreach (var reRoute in _options.Value.ReRoutes)
                {
                    SetUpReRoute(reRoute);
                }
            }
        }

        private void SetUpReRoute(YamlReRoute reRoute)
        {
            var upstreamTemplate = reRoute.UpstreamTemplate;

            var placeholders = new List<string>();

            for (int i = 0; i < upstreamTemplate.Length; i++)
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

                _reRoutes.Add(new ReRoute(reRoute.DownstreamTemplate, reRoute.UpstreamTemplate,
                    reRoute.UpstreamHttpMethod, upstreamTemplate, isAuthenticated, authOptionsForRoute
                    ));
            }
            else
            {
                _reRoutes.Add(new ReRoute(reRoute.DownstreamTemplate, reRoute.UpstreamTemplate, reRoute.UpstreamHttpMethod,
                    upstreamTemplate, isAuthenticated, null));
            }
        }

        private bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }

        public List<ReRoute> ReRoutes => _reRoutes;
    }
}