using Ocelot.Configuration.File;
using Ocelot.RateLimiting;

namespace Ocelot.Configuration.Creator;

public class RateLimitOptionsCreator : IRateLimitOptionsCreator
{
    private readonly IRateLimiting _rateLimiting;

    public RateLimitOptionsCreator(IRateLimiting limiting)
    {
        _rateLimiting = limiting;
    }

    public RateLimitOptions Create(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);

        var rule = route.RateLimitOptions ?? new();
        var global = globalConfiguration.RateLimitOptions ?? new();
        return rule.EnableRateLimiting ?
            new()
            {
                ClientIdHeader = global.ClientIdHeader,
                ClientWhitelist = rule.ClientWhitelist ?? global.ClientWhitelist ?? GlobalClientWhitelist(),
                EnableHeaders = global.DisableRateLimitHeaders.HasValue ? !global.DisableRateLimitHeaders.Value : global.EnableHeaders,
                EnableRateLimiting = rule.EnableRateLimiting,
                HttpStatusCode = global.HttpStatusCode,
                QuotaExceededMessage = global.QuotaExceededMessage,
                RateLimitCounterPrefix = global.RateLimitCounterPrefix,
                RateLimitRule = new(rule.Period, rule.PeriodTimespan, rule.Limit),
            }
            : CreatePatternRules(route, globalConfiguration);
    }

    public RateLimitOptions CreatePatternRules(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);

        var path = route.UpstreamPathTemplate ?? string.Empty;
        var methods = route.UpstreamHttpMethod ?? []; // limiting downstream HTTP verbs has no effect; only upstream methods are respected, also keep in mind Method Transformation feature
        var globalRule = globalConfiguration.RateLimitingRules
            .FirstOrDefault(rule => Regex.IsMatch(path, '^' + Regex.Escape(rule.Pattern).Replace("\\*", ".*") + '$', RegexOptions.IgnoreCase | RegexOptions.Compiled));
        if (globalRule != null)
        {
            return new RateLimitOptions()
            {
                EnableHeaders = globalRule.DisableRateLimitHeaders.HasValue ? !globalRule.DisableRateLimitHeaders.Value : globalRule.EnableHeaders,
                EnableRateLimiting = globalRule.EnableRateLimiting,
                HttpStatusCode = globalRule.HttpStatusCode,
                QuotaExceededMessage = globalRule.QuotaExceededMessage,
                RateLimitRule = new(globalRule.Period, globalRule.PeriodTimespan, globalRule.Limit),
                ClientWhitelist = globalRule.ClientWhitelist ?? GlobalClientWhitelist(),
            };
        }

        return new() { EnableRateLimiting = false };
    }

    protected virtual List<string> GlobalClientWhitelist() => new();
}
