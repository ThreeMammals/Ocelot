﻿using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Creator;
using Ocelot.Infrastructure;

namespace Ocelot.Configuration.Validator;

/// <summary>
/// Default implementation od the <see cref="AbstractValidator{FileRoute}"/> abstract class.
/// </summary>
public partial class RouteFluentValidator : AbstractValidator<FileRoute>
{
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public RouteFluentValidator(IAuthenticationSchemeProvider authenticationSchemeProvider, HostAndPortValidator hostAndPortValidator, FileQoSOptionsFluentValidator fileQoSOptionsFluentValidator)
    {
        _authenticationSchemeProvider = authenticationSchemeProvider;

        RuleFor(route => route.QoSOptions)
            .SetValidator(fileQoSOptionsFluentValidator);

        RuleFor(route => route.DownstreamPathTemplate)
            .NotEmpty()
            .WithMessage("{PropertyName} cannot be empty");

        RuleFor(route => route.UpstreamPathTemplate)
            .NotEmpty()
            .WithMessage("{PropertyName} cannot be empty");

        When(route => !string.IsNullOrEmpty(route.DownstreamPathTemplate), () =>
        {
            RuleFor(route => route.DownstreamPathTemplate)
                .Must(path => path.StartsWith('/'))
                .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

            RuleFor(route => route.DownstreamPathTemplate)
                .Must(path => !path.Contains("//"))
                .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

            RuleFor(route => route.DownstreamPathTemplate)
                .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                .WithMessage("{PropertyName} {PropertyValue} contains scheme");
        });

        When(route => !string.IsNullOrEmpty(route.UpstreamPathTemplate), () =>
        {
            RuleFor(route => route.UpstreamPathTemplate)
                .Must(path => !path.Contains("//"))
                .WithMessage("{PropertyName} {PropertyValue} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature.");

            RuleFor(route => route.UpstreamPathTemplate)
                .Must(path => path.StartsWith('/'))
                .WithMessage("{PropertyName} {PropertyValue} doesnt start with forward slash");

            RuleFor(route => route.UpstreamPathTemplate)
                .Must(path => !path.Contains("https://") && !path.Contains("http://"))
                .WithMessage("{PropertyName} {PropertyValue} contains scheme");
        });

        When(route => route.RateLimitOptions.EnableRateLimiting, () =>
        {
            RuleFor(route => route.RateLimitOptions.Period)
                .NotEmpty()
                .WithMessage("RateLimitOptions.Period is empty");

            RuleFor(route => route.RateLimitOptions)
                .Must(IsValidPeriod)
                .WithMessage("RateLimitOptions.Period does not contain integer then s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period");
        });

        RuleFor(route => route.AuthenticationOptions)
            .MustAsync(IsSupportedAuthenticationProviders)
            .WithMessage("{PropertyName} {PropertyValue} is unsupported authentication provider");

        When(route => string.IsNullOrEmpty(route.ServiceName), () =>
        {
            RuleFor(r => r.DownstreamHostAndPorts).NotEmpty()
                .WithMessage("When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!");
        });

        When(route => string.IsNullOrEmpty(route.ServiceName), () =>
        {
            RuleForEach(route => route.DownstreamHostAndPorts)
                .SetValidator(hostAndPortValidator);
        });

        When(route => !string.IsNullOrEmpty(route.DownstreamHttpVersion), () =>
        {
            RuleFor(r => r.DownstreamHttpVersion).Matches("^[0-9]([.,][0-9]{1,1})?$");
        });

        When(route => !string.IsNullOrEmpty(route.DownstreamHttpVersionPolicy), () =>
        {
            RuleFor(r => r.DownstreamHttpVersionPolicy).Matches($@"^({VersionPolicies.RequestVersionExact}|{VersionPolicies.RequestVersionOrHigher}|{VersionPolicies.RequestVersionOrLower})$");
        });
    }

    private async Task<bool> IsSupportedAuthenticationProviders(FileAuthenticationOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.AuthenticationProviderKey)
            && options.AuthenticationProviderKeys.Length == 0)
        {
            return true;
        }

        var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        var supportedSchemes = schemes.Select(scheme => scheme.Name).ToList();
        var primary = options.AuthenticationProviderKey;
        return !string.IsNullOrEmpty(primary) && supportedSchemes.Contains(primary)
            || (string.IsNullOrEmpty(primary) && options.AuthenticationProviderKeys.All(supportedSchemes.Contains));
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex("^[0-9]+s", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex SecondsRegex();
    [GeneratedRegex("^[0-9]+m", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex MinutesRegex();
    [GeneratedRegex("^[0-9]+h", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex HoursRegex();
    [GeneratedRegex("^[0-9]+d", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex DaysRegex();
#else
    private static readonly Regex _secondsRegex = RegexGlobal.New("^[0-9]+s");
    private static readonly Regex _minutesRegex = RegexGlobal.New("^[0-9]+m");
    private static readonly Regex _hoursRegex = RegexGlobal.New("^[0-9]+h");
    private static readonly Regex _daysRegex = RegexGlobal.New("^[0-9]+d");
    private static Regex SecondsRegex() => _secondsRegex;
    private static Regex MinutesRegex() => _minutesRegex;
    private static Regex HoursRegex() => _hoursRegex;
    private static Regex DaysRegex() => _daysRegex;
#endif

    private static bool IsValidPeriod(FileRateLimitRule rateLimitOptions)
    {
        if (string.IsNullOrEmpty(rateLimitOptions.Period))
        {
            return false;
        }

        var period = rateLimitOptions.Period.Trim();
        return SecondsRegex().Match(period).Success
               || MinutesRegex().Match(period).Success
               || HoursRegex().Match(period).Success
               || DaysRegex().Match(period).Success;
    }
}
