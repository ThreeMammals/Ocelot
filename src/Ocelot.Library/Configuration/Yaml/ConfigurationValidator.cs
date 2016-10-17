namespace Ocelot.Library.Configuration.Yaml
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Authentication;
    using Errors;
    using Responses;

    public class ConfigurationValidator : IConfigurationValidator
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
            SupportAuthenticationProviders supportedProvider;

            return Enum.TryParse(provider, true, out supportedProvider);
        }

        private ConfigurationValidationResult CheckForDupliateReRoutes(YamlConfiguration configuration)
        {
            var duplicateUpstreamTemplates = configuration.ReRoutes
                .Select(r => r.DownstreamTemplate)
                .GroupBy(r => r)
                .Where(r => r.Count() > 1)
                .Select(r => r.Key)
                .ToList();

            if (duplicateUpstreamTemplates.Count <= 0)
            {
                return new ConfigurationValidationResult(false);
            }

            var errors = duplicateUpstreamTemplates
                .Select(duplicateUpstreamTemplate => new DownstreamTemplateAlreadyUsedError(string.Format("Duplicate DownstreamTemplate: {0}", duplicateUpstreamTemplate)))
                .Cast<Error>()
                .ToList();

            return new ConfigurationValidationResult(true, errors);
        }
    }
}
