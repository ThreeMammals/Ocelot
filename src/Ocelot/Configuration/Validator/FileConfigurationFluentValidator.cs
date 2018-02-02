using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;
using Ocelot.Errors;
using Ocelot.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.Configuration.Validator
{
    public class FileConfigurationFluentValidator : AbstractValidator<FileConfiguration>, IConfigurationValidator
    {
        public FileConfigurationFluentValidator(IAuthenticationSchemeProvider authenticationSchemeProvider)
        {
            RuleFor(configuration => configuration.ReRoutes)
                .SetCollectionValidator(new ReRouteFluentValidator(authenticationSchemeProvider));
                
            RuleForEach(configuration => configuration.ReRoutes)
                .Must((config, reRoute) => IsNotDuplicateIn(reRoute, config.ReRoutes))
                .WithMessage((config, reRoute) => $"{nameof(reRoute)} {reRoute.UpstreamPathTemplate} has duplicate");
        }

        public async Task<Response<ConfigurationValidationResult>> IsValid(FileConfiguration configuration)
        {
            var validateResult = await ValidateAsync(configuration);

            if (validateResult.IsValid)
            {
                return new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false));
            }

            var errors = validateResult.Errors.Select(failure => new FileValidationFailedError(failure.ErrorMessage));

            var result = new ConfigurationValidationResult(true, errors.Cast<Error>().ToList());

            return new OkResponse<ConfigurationValidationResult>(result);
        }

        private static bool IsNotDuplicateIn(FileReRoute reRoute, List<FileReRoute> reRoutes)
        {
            var matchingReRoutes = reRoutes
                .Where(r => r.UpstreamPathTemplate == reRoute.UpstreamPathTemplate && (r.UpstreamHost != reRoute.UpstreamHost || reRoute.UpstreamHost == null)).ToList();

            if(matchingReRoutes.Count == 1)
            {
                return true;
            }

            var allowAllVerbs = matchingReRoutes.Any(x => x.UpstreamHttpMethod.Count == 0);

            var duplicateAllowAllVerbs = matchingReRoutes.Count(x => x.UpstreamHttpMethod.Count == 0) > 1;

            var specificVerbs = matchingReRoutes.Any(x => x.UpstreamHttpMethod.Count != 0);

            var duplicateSpecificVerbs = matchingReRoutes.SelectMany(x => x.UpstreamHttpMethod).GroupBy(x => x.ToLower()).SelectMany(x => x.Skip(1)).Any();

            if (duplicateAllowAllVerbs || duplicateSpecificVerbs || (allowAllVerbs && specificVerbs))
            {
                return false;
            }

            return true;
        }
    }
}
