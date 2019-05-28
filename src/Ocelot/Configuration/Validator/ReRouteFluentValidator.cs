namespace Ocelot.Configuration.Validator
{
    using File;
    using FluentValidation;
    using Microsoft.AspNetCore.Authentication;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class ReRouteFluentValidator : AbstractValidator<FileReRoute>
    {
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        public ReRouteFluentValidator(IAuthenticationSchemeProvider authenticationSchemeProvider, HostAndPortValidator hostAndPortValidator, FileQoSOptionsFluentValidator fileQoSOptionsFluentValidator)
        {
            _authenticationSchemeProvider = authenticationSchemeProvider;

            RuleFor(reRoute => reRoute.QoSOptions)
                .SetValidator(fileQoSOptionsFluentValidator);

            RuleFor(reRoute => reRoute.DownstreamPathTemplate)
                .NotEmpty()
                .WithMessage("{PropertyName} cannot be empty");

            RuleFor(reRoute => reRoute.UpstreamPathTemplate)
                .NotEmpty()
                .WithMessage("{PropertyName} cannot be empty");

            When(reRoute => !string.IsNullOrEmpty(reRoute.DownstreamPathTemplate), () =>
            {
                RuleFor(reRoute => reRoute.DownstreamPathTemplate)
                    .Must(path => path.StartsWith("/"))
                    .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

                RuleFor(reRoute => reRoute.DownstreamPathTemplate)
                    .Must(path => !path.Contains("//"))
                    .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

                RuleFor(reRoute => reRoute.DownstreamPathTemplate)
                    .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                    .WithMessage("{PropertyName} {PropertyValue} contains scheme");
            });

            When(reRoute => !string.IsNullOrEmpty(reRoute.UpstreamPathTemplate), () =>
            {
                RuleFor(reRoute => reRoute.UpstreamPathTemplate)
                    .Must(path => !path.Contains("//"))
                    .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

                RuleFor(reRoute => reRoute.UpstreamPathTemplate)
                    .Must(path => path.StartsWith("/"))
                    .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

                RuleFor(reRoute => reRoute.UpstreamPathTemplate)
                    .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                    .WithMessage("{PropertyName} {PropertyValue} contains scheme");
            });

            When(reRoute => reRoute.RateLimitOptions.EnableRateLimiting, () =>
            {
                RuleFor(reRoute => reRoute.RateLimitOptions.Period)
                    .NotEmpty()
                    .WithMessage("RateLimitOptions.Period is empty");

                RuleFor(reRoute => reRoute.RateLimitOptions)
                    .Must(IsValidPeriod)
                    .WithMessage("RateLimitOptions.Period does not contain integer then s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period");
            });

            RuleFor(reRoute => reRoute.AuthenticationOptions)
                .MustAsync(IsSupportedAuthenticationProviders)
                .WithMessage("{PropertyName} {PropertyValue} is unsupported authentication provider");

            When(reRoute => string.IsNullOrEmpty(reRoute.ServiceName), () =>
            {
                RuleFor(r => r.DownstreamHostAndPorts).NotEmpty()
                    .WithMessage("When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!");
            });

            When(reRoute => string.IsNullOrEmpty(reRoute.ServiceName), () =>
            {
                RuleFor(reRoute => reRoute.DownstreamHostAndPorts)
                    .SetCollectionValidator(hostAndPortValidator);
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
            if (string.IsNullOrEmpty(rateLimitOptions.Period))
            {
                return false;
            }

            var period = rateLimitOptions.Period;

            var secondsRegEx = new Regex("^[0-9]+s");
            var minutesRegEx = new Regex("^[0-9]+m");
            var hoursRegEx = new Regex("^[0-9]+h");
            var daysRegEx = new Regex("^[0-9]+d");

            return secondsRegEx.Match(period).Success
                   || minutesRegEx.Match(period).Success
                   || hoursRegEx.Match(period).Success
                   || daysRegEx.Match(period).Success;
        }
    }
}
