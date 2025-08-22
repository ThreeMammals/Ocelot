using Ocelot.Configuration.Builder;
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
        var methods = route.UpstreamHttpMethod ?? []; // limiting downstream HTTP verbs has no effect; only upstream methods are respected, also keep in mind Method Transformation feature
        var globalRule = globalConfiguration.RateLimitingRules
            .FirstOrDefault(rule => Regex.IsMatch(path, '^' + Regex.Escape(rule.Pattern).Replace("\\*", ".*") + '$', RegexOptions.IgnoreCase | RegexOptions.Compiled)
                && (methods.Count == 0 || rule.Methods.Count == 0 || rule.Methods.Intersect(methods).Any()));
        if (globalRule != null)
        {
            return new RateLimitOptionsBuilder()
                .WithDisableRateLimitHeaders(globalRule.DisableRateLimitHeaders)
                .WithEnableRateLimiting(globalRule.EnableRateLimiting)
                .WithHttpStatusCode(globalRule.HttpStatusCode)
                .WithQuotaExceededMessage(globalRule.QuotaExceededMessage)
                .WithRateLimitRule(new RateLimitRule(
                    globalRule.Period,
                    _rateLimiting.ToTimespan(globalRule.Period).TotalSeconds, // TODO This is design issue because of parsing Period! The 2nd parameter must be FileRateLimitRule.PeriodTimespan !!!
                    globalRule.Limit))
                .WithClientWhiteList(() => [])
                .Build();
        }

        return new RateLimitOptionsBuilder().WithEnableRateLimiting(false).Build();
    }
}
