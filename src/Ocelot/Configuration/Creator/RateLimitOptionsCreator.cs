using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class RateLimitOptionsCreator : IRateLimitOptionsCreator
{
    public RateLimitOptions Create(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration)
    {
        var rule = route?.RateLimitOptions ?? new();
        if (rule.EnableRateLimiting)
        {
            var global = globalConfiguration?.RateLimitOptions ?? new();
            return new RateLimitOptionsBuilder()
                .WithClientIdHeader(global.ClientIdHeader)
                .WithClientWhiteList(() => rule.ClientWhitelist)
                .WithDisableRateLimitHeaders(global.DisableRateLimitHeaders)
                .WithEnableRateLimiting(rule.EnableRateLimiting)
                .WithHttpStatusCode(global.HttpStatusCode)
                .WithQuotaExceededMessage(global.QuotaExceededMessage)
                .WithRateLimitCounterPrefix(global.RateLimitCounterPrefix)
                .WithRateLimitRule(new RateLimitRule(rule.Period, rule.PeriodTimespan, rule.Limit))
                .Build();
        }

        return CreatePatternRules(route, globalConfiguration);
    }

    public RateLimitOptions CreatePatternRules(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);

        var path = route.UpstreamPathTemplate ?? string.Empty;
        var methods = route.UpstreamHttpMethod ?? new(); // limiting downstream HTTP verbs has no effect; only upstream methods are respected, also keep in mind Method Transformation feature
        var globalRule = globalConfiguration.RateLimitingRules
            .FirstOrDefault(rule => Regex.IsMatch(path, "^" + Regex.Escape(rule.Pattern).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                && (methods.Count == 0 || rule.Methods.Count == 0 || rule.Methods.Intersect(methods).Any()));
        if (globalRule != null)
        {
            return new RateLimitOptionsBuilder()
                .WithDisableRateLimitHeaders(globalRule.DisableRateLimitHeaders)
                .WithEnableRateLimiting(globalRule.EnableRateLimiting) // TODO Double check this -> EnableEndpointEndpointRateLimiting = true;
                .WithHttpStatusCode(globalRule.HttpStatusCode)
                .WithQuotaExceededMessage(globalRule.QuotaExceededMessage)
                .WithRateLimitRule(new RateLimitRule(
                    globalRule.Period,
                    ParsePeriodTimespan(globalRule.Period), // TODO Review this, it seems this is the new feature, we parse Period only.
                    globalRule.Limit))
                .WithClientWhiteList(() => [])
                .Build();
        }

        return new RateLimitOptionsBuilder().WithEnableRateLimiting(false).Build();
    }

    private static double ParsePeriodTimespan(string period)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period, nameof(period));

        char unit = period[^1]; // separate latest character
        string numberPart = period[..^1]; // all characters except latest
        if (!double.TryParse(numberPart, out var value))
        {
            throw new ArgumentException($"Invalid period number: {period}", nameof(period));
        }

        return unit switch
        {
            's' => value, // seconds
            'm' => value * 60, // minutes
            'h' => value * 3600, // hour
            'd' => value * 86400, // day
            _ => throw new ArgumentException($"Invalid period unit: {unit}", nameof(period)),
        };
    }
}
