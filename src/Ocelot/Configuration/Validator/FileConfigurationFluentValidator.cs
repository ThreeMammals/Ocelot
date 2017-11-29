using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Configuration.Validator
{
    public class FileConfigurationFluentValidator : AbstractValidator<FileConfiguration>, IConfigurationValidator
    {
        public FileConfigurationFluentValidator(IAuthenticationSchemeProvider authenticationSchemeProvider)
        {
            RuleFor(configuration => configuration.ReRoutes)
                .SetCollectionValidator(new ReRouteFluentValidator(authenticationSchemeProvider));
            RuleForEach(configuration => configuration.ReRoutes)
                .Must((config, reRoute) => !IsDuplicateIn(reRoute, config.ReRoutes))
                .WithMessage((config, reRoute) => $"duplicate downstreampath {reRoute.UpstreamPathTemplate}");
        }

        private static bool IsDuplicateIn(FileReRoute reRoute, List<FileReRoute> routes)
        {
            var reRoutesWithUpstreamPathTemplate = routes.Where(r => r.UpstreamPathTemplate == reRoute.UpstreamPathTemplate).ToList();
            var hasEmptyListToAllowAllHttpVerbs = reRoutesWithUpstreamPathTemplate.Any(x => x.UpstreamHttpMethod.Count == 0);
            var hasDuplicateEmptyListToAllowAllHttpVerbs = reRoutesWithUpstreamPathTemplate.Count(x => x.UpstreamHttpMethod.Count == 0) > 1;

            var hasSpecificHttpVerbs = reRoutesWithUpstreamPathTemplate.Any(x => x.UpstreamHttpMethod.Count != 0);
            var hasDuplicateSpecificHttpVerbs = reRoutesWithUpstreamPathTemplate.SelectMany(x => x.UpstreamHttpMethod).GroupBy(x => x.ToLower()).SelectMany(x => x.Skip(1)).Any();
            if (hasDuplicateEmptyListToAllowAllHttpVerbs || hasDuplicateSpecificHttpVerbs || (hasEmptyListToAllowAllHttpVerbs && hasSpecificHttpVerbs))
            {
                return true;
            }
            return false;
        }

        public async Task<Response<ConfigurationValidationResult>> IsValid(FileConfiguration configuration)
        {
            var validateResult = await ValidateAsync(configuration);
            if (validateResult.IsValid)
            {
                return new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false));
            }
            var errors = validateResult.Errors.Select(failure => new FileValidationFailedError(failure.ErrorMessage));
            return new ErrorResponse<ConfigurationValidationResult>(errors.Cast<Error>().ToList());
        }
    }
}
