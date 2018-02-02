using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Configuration.Validator
{
    public class ReRouteFluentValidator : AbstractValidator<FileReRoute>
    {
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        public ReRouteFluentValidator(IAuthenticationSchemeProvider authenticationSchemeProvider)
        {
            _authenticationSchemeProvider = authenticationSchemeProvider;

            RuleFor(reRoute => reRoute.DownstreamPathTemplate)
                .Must(path => path.StartsWith("/"))
                .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

            RuleFor(reRoute => reRoute.UpstreamPathTemplate)
                .Must(path => !path.Contains("//"))
                .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

            RuleFor(reRoute => reRoute.DownstreamPathTemplate)
                .Must(path => !path.Contains("//"))
                .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

            RuleFor(reRoute => reRoute.UpstreamPathTemplate)
                .Must(path => path.StartsWith("/"))
                .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

            RuleFor(reRoute => reRoute.DownstreamPathTemplate)
                .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                .WithMessage("{PropertyName} {PropertyValue} contains scheme");

            RuleFor(reRoute => reRoute.UpstreamPathTemplate)
                .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                .WithMessage("{PropertyName} {PropertyValue} contains scheme");

            RuleFor(reRoute => reRoute.RateLimitOptions)
                .Must(IsValidPeriod)
                .WithMessage("RateLimitOptions.Period does not contains (s,m,h,d)");
                
            RuleFor(reRoute => reRoute.AuthenticationOptions)
                .MustAsync(IsSupportedAuthenticationProviders)
                .WithMessage("{PropertyValue} is unsupported authentication provider");

            When(reRoute => reRoute.UseServiceDiscovery, () => {
                RuleFor(r => r.ServiceName).NotEmpty().WithMessage("ServiceName cannot be empty or null when using service discovery or Ocelot cannot look up your service!");
                });

            When(reRoute => !reRoute.UseServiceDiscovery, () => {
                RuleFor(r => r.DownstreamHostAndPorts).NotEmpty().WithMessage("When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!");
            });

            When(reRoute => !reRoute.UseServiceDiscovery, () => {
                RuleFor(reRoute => reRoute.DownstreamHostAndPorts)
                    .SetCollectionValidator(new HostAndPortValidator());
            });
        }

        private async Task<bool> IsSupportedAuthenticationProviders(FileAuthenticationOptions authenticationOptions, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(authenticationOptions.AuthenticationProviderKey))
            {
                return true;
            }
            var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();

            var supportedSchemes = schemes.Select(scheme => scheme.Name).ToList();

            return supportedSchemes.Contains(authenticationOptions.AuthenticationProviderKey);
        }

        private static bool IsValidPeriod(FileRateLimitRule rateLimitOptions)
        {
            string period = rateLimitOptions.Period;

            return !rateLimitOptions.EnableRateLimiting || period.Contains("s") || period.Contains("m") || period.Contains("h") || period.Contains("d");
        }
    }
}
