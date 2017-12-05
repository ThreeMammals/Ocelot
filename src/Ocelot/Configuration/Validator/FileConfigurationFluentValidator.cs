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
                .WithMessage((config, reRoute) => $"duplicate downstreampath {reRoute.UpstreamPathTemplate}");
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

        private static bool IsNotDuplicateIn(FileReRoute reRoute, List<FileReRoute> routes)
        {
            var reRoutesWithUpstreamPathTemplate = routes.Where(r => r.UpstreamPathTemplate == reRoute.UpstreamPathTemplate).ToList();
            var hasEmptyListToAllowAllHttpVerbs = reRoutesWithUpstreamPathTemplate.Any(x => x.UpstreamHttpMethod.Count == 0);
            var hasDuplicateEmptyListToAllowAllHttpVerbs = reRoutesWithUpstreamPathTemplate.Count(x => x.UpstreamHttpMethod.Count == 0) > 1;

            var hasSpecificHttpVerbs = reRoutesWithUpstreamPathTemplate.Any(x => x.UpstreamHttpMethod.Count != 0);
            var hasDuplicateSpecificHttpVerbs = reRoutesWithUpstreamPathTemplate.SelectMany(x => x.UpstreamHttpMethod).GroupBy(x => x.ToLower()).SelectMany(x => x.Skip(1)).Any();
            if (hasDuplicateEmptyListToAllowAllHttpVerbs || hasDuplicateSpecificHttpVerbs || (hasEmptyListToAllowAllHttpVerbs && hasSpecificHttpVerbs))
            {
                return false;
            }
            return true;
        }
    }
}
