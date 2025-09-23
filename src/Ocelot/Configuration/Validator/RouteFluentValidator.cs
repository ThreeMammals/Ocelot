using FluentValidation;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;

namespace Ocelot.Configuration.Validator;

/// <summary>
/// Default implementation od the <see cref="AbstractValidator{FileRoute}"/> abstract class.
/// </summary>
public partial class RouteFluentValidator : AbstractValidator<FileRoute>
{
    public RouteFluentValidator(
        HostAndPortValidator hostAndPortValidator,
        FileQoSOptionsFluentValidator qosOptsValidator,
        FileAuthenticationOptionsValidator authOptsValidator)
    {
        RuleFor(route => route.QoSOptions)
            .SetValidator(qosOptsValidator);

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

        When(route => route.RateLimitOptions != null && route.RateLimitOptions.EnableRateLimiting != false, () =>
        {
            RuleFor(route => route.RateLimitOptions.Limit)
                .Must(limit => !limit.HasValue || (limit.HasValue && limit.Value > 0))
                .WithMessage(route => $"RateLimitOptions.Limit is negative or zero for the route {route}");
            RuleFor(route => route.RateLimitOptions.Period)
                .NotEmpty()
                .WithMessage("RateLimitOptions.Period is empty");

            RuleFor(route => route.RateLimitOptions)
                .Must(IsValidPeriod)
                .WithMessage("RateLimitOptions.Period does not contain integer then ms (millisecond), s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period");
        });

        RuleFor(route => route.AuthenticationOptions)
            .SetValidator(authOptsValidator);

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

    [GeneratedRegex(@"^\d+(\.\d+)?ms", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex MilliSecondsRegex();

    [GeneratedRegex(@"^\d+(\.\d+)?s", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex SecondsRegex();

    [GeneratedRegex(@"^\d+(\.\d+)?m", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex MinutesRegex();

    [GeneratedRegex(@"^\d+(\.\d+)?h", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex HoursRegex();

    [GeneratedRegex(@"^\d+(\.\d+)?d", RegexOptions.None, RegexGlobal.DefaultMatchTimeoutMilliseconds)]
    private static partial Regex DaysRegex();

    private static bool IsValidPeriod(FileRateLimitByHeaderRule rateLimitOptions)
    {
        if (string.IsNullOrEmpty(rateLimitOptions.Period))
        {
            return false;
        }

        var period = rateLimitOptions.Period.Trim();
        return MilliSecondsRegex().Match(period).Success
               || SecondsRegex().Match(period).Success
               || MinutesRegex().Match(period).Success
               || HoursRegex().Match(period).Success
               || DaysRegex().Match(period).Success;
    }
}
