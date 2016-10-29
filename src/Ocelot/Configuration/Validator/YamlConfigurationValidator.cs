using System;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Authentication.Handler;
using Ocelot.Configuration.Yaml;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Configuration.Validator
{
    public class YamlConfigurationValidator : IConfigurationValidator
    {
        public Response<ConfigurationValidationResult> IsValid(YamlConfiguration configuration)
        {
            var result = CheckForDupliateReRoutes(configuration);

            if (result.IsError)
            {
                return new OkResponse<ConfigurationValidationResult>(result);
            }

            result = CheckForUnsupportedAuthenticationProviders(configuration);

            if (result.IsError)
            {
                return new OkResponse<ConfigurationValidationResult>(result);
            }

            return new OkResponse<ConfigurationValidationResult>(result);
        }

        private ConfigurationValidationResult CheckForUnsupportedAuthenticationProviders(YamlConfiguration configuration)
        {
            var errors = new List<Error>();

            foreach (var yamlReRoute in configuration.ReRoutes)
            {
                var isAuthenticated = !string.IsNullOrEmpty(yamlReRoute.AuthenticationOptions?.Provider);

                if (!isAuthenticated)
                {
                    continue;
                }

                if (IsSupportedAuthenticationProvider(yamlReRoute.AuthenticationOptions?.Provider))
                {
                    continue;
                }

                var error = new UnsupportedAuthenticationProviderError($"{yamlReRoute.AuthenticationOptions?.Provider} is unsupported authentication provider, upstream template is {yamlReRoute.UpstreamTemplate}, upstream method is {yamlReRoute.UpstreamHttpMethod}");
                errors.Add(error);
            }

            return errors.Count > 0 
                ? new ConfigurationValidationResult(true, errors) 
                : new ConfigurationValidationResult(false);
        }

        private bool IsSupportedAuthenticationProvider(string provider)
        {
            SupportedAuthenticationProviders supportedProvider;

            return Enum.TryParse(provider, true, out supportedProvider);
        }

        private ConfigurationValidationResult CheckForDupliateReRoutes(YamlConfiguration configuration)
        {
            var hasDupes = configuration.ReRoutes
                   .GroupBy(x => new { x.UpstreamTemplate, x.UpstreamHttpMethod }).Any(x => x.Skip(1).Any());

            if (!hasDupes)
            {
                return new ConfigurationValidationResult(false);
            }

            var dupes = configuration.ReRoutes.GroupBy(x => new { x.UpstreamTemplate, x.UpstreamHttpMethod })
                               .Where(x => x.Skip(1).Any());

            var errors = dupes
                .Select(d => new DownstreamTemplateAlreadyUsedError(string.Format("Duplicate DownstreamTemplate: {0}", d.Key.UpstreamTemplate)))
                .Cast<Error>()
                .ToList();

            return new ConfigurationValidationResult(true, errors);
        }
    }
}
