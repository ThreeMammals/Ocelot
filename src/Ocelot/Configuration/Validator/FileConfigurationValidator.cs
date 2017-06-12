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
            var result = CheckForDuplicateReRoutes(configuration);

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
            result = CheckForReRoutesRateLimitOptions(configuration);

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

        private ConfigurationValidationResult CheckForDuplicateReRoutes(FileConfiguration configuration)
        {         
            var duplicatedUpstreamPathTemplates = new List<string>();

            var distinctUpstreamPathTemplates = configuration.ReRoutes.Select(x => x.UpstreamPathTemplate).Distinct();
            
            foreach (string upstreamPathTemplate in distinctUpstreamPathTemplates)
            {
                var reRoutesWithUpstreamPathTemplate = configuration.ReRoutes.Where(x => x.UpstreamPathTemplate == upstreamPathTemplate);

                var hasEmptyListToAllowAllHttpVerbs = reRoutesWithUpstreamPathTemplate.Where(x => x.UpstreamHttpMethod.Count() == 0).Any();
                var hasDuplicateEmptyListToAllowAllHttpVerbs = reRoutesWithUpstreamPathTemplate.Where(x => x.UpstreamHttpMethod.Count() == 0).Count() > 1;
                var hasSpecificHttpVerbs = reRoutesWithUpstreamPathTemplate.Where(x => x.UpstreamHttpMethod.Count() > 0).Any();
                var hasDuplicateSpecificHttpVerbs = reRoutesWithUpstreamPathTemplate.SelectMany(x => x.UpstreamHttpMethod).GroupBy(x => x.ToLower()).SelectMany(x => x.Skip(1)).Any();

                if (hasDuplicateEmptyListToAllowAllHttpVerbs || hasDuplicateSpecificHttpVerbs || (hasEmptyListToAllowAllHttpVerbs && hasSpecificHttpVerbs))
                {
                    duplicatedUpstreamPathTemplates.Add(upstreamPathTemplate);
                }
            }

            if (duplicatedUpstreamPathTemplates.Count() == 0)
            {
                return new ConfigurationValidationResult(false);
            }
            else
            {
                var errors = duplicatedUpstreamPathTemplates
                    .Select(d => new DownstreamPathTemplateAlreadyUsedError(string.Format("Duplicate DownstreamPath: {0}", d)))
                    .Cast<Error>()
                    .ToList();

                return new ConfigurationValidationResult(true, errors);
            }

        }

        private ConfigurationValidationResult CheckForReRoutesRateLimitOptions(FileConfiguration configuration)
        {
            var errors = new List<Error>();

            foreach (var reRoute in configuration.ReRoutes)
            {
                if (reRoute.RateLimitOptions.EnableRateLimiting)
                {
                    if (!IsValidPeriod(reRoute))
                    {
                        errors.Add(new RateLimitOptionsValidationError($"{reRoute.RateLimitOptions.Period} not contains scheme"));
                    }
                }
            }

            if (errors.Any())
            {
                return new ConfigurationValidationResult(true, errors);
            }

            return new ConfigurationValidationResult(false, errors);
        }

        private static bool IsValidPeriod(FileReRoute reRoute)
        {
            string period = reRoute.RateLimitOptions.Period;

            return period.Contains("s") || period.Contains("m") || period.Contains("h") || period.Contains("d");
        }
    }
}
