using System;
using System.Collections.Generic;
using System.Linq;
using Ocelot.Authentication.Handler;
using Ocelot.Configuration.File;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Configuration.Validator
{
    public class FileConfigurationValidator : IConfigurationValidator
    {
        public Response<ConfigurationValidationResult> IsValid(FileConfiguration configuration)
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

            result = CheckForReRoutesContainingDownstreamSchemeInDownstreamPathTemplate(configuration);

            if (result.IsError)
            {
                return new OkResponse<ConfigurationValidationResult>(result);
            }

            return new OkResponse<ConfigurationValidationResult>(result);
        }

        private ConfigurationValidationResult CheckForUnsupportedAuthenticationProviders(FileConfiguration configuration)
        {
            var errors = new List<Error>();

            foreach (var reRoute in configuration.ReRoutes)
            {
                var isAuthenticated = !string.IsNullOrEmpty(reRoute.AuthenticationOptions?.Provider);

                if (!isAuthenticated)
                {
                    continue;
                }

                if (IsSupportedAuthenticationProvider(reRoute.AuthenticationOptions?.Provider))
                {
                    continue;
                }

                var error = new UnsupportedAuthenticationProviderError($"{reRoute.AuthenticationOptions?.Provider} is unsupported authentication provider, upstream template is {reRoute.UpstreamPathTemplate}, upstream method is {reRoute.UpstreamHttpMethod}");
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

        private ConfigurationValidationResult CheckForReRoutesContainingDownstreamSchemeInDownstreamPathTemplate(FileConfiguration configuration)
        {   
            var errors = new List<Error>();

            foreach(var reRoute in configuration.ReRoutes)
            {
                if(reRoute.DownstreamPathTemplate.Contains("https://")
                || reRoute.DownstreamPathTemplate.Contains("http://"))
                {
                    errors.Add(new DownstreamPathTemplateContainsSchemeError($"{reRoute.DownstreamPathTemplate} contains scheme"));
                }
            }

            if(errors.Any())
            {
                return new ConfigurationValidationResult(true, errors);
            }

            return new ConfigurationValidationResult(false, errors);
        }

        private ConfigurationValidationResult CheckForDupliateReRoutes(FileConfiguration configuration)
        {
            var hasDupes = configuration.ReRoutes
                   .GroupBy(x => new { x.UpstreamPathTemplate, x.UpstreamHttpMethod }).Any(x => x.Skip(1).Any());

            if (!hasDupes)
            {
                return new ConfigurationValidationResult(false);
            }

            var dupes = configuration.ReRoutes.GroupBy(x => new { x.UpstreamPathTemplate, x.UpstreamHttpMethod })
                               .Where(x => x.Skip(1).Any());

            var errors = dupes
                .Select(d => new DownstreamPathTemplateAlreadyUsedError(string.Format("Duplicate DownstreamPath: {0}", d.Key.UpstreamPathTemplate)))
                .Cast<Error>()
                .ToList();

            return new ConfigurationValidationResult(true, errors);
        }
    }
}
