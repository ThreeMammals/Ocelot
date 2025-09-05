using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
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

        var rule = route.RateLimitOptions;
        var global = globalConfiguration.RateLimitOptions;
        bool isGlobal = global?.RouteKeys?.Contains(route.Key) ?? true;
        if (rule?.EnableRateLimiting == false || (isGlobal && global?.EnableRateLimiting == false))
        {
            return new(false);
        }

        if (globalConfiguration.RateLimitingRules?.Count > 0)
        {
            return CreatePatternRules(route, globalConfiguration);
        }

        if (rule != null && global == null)
        {
            return new(rule);
        }
        else if (rule == null && global != null && isGlobal)
        {
            return new(global);
        }
        else if (rule != null && global != null && !isGlobal)
        {
            return new(rule);
        }
        else if (rule != null && global != null && isGlobal)
        {
            return MergeRateLimitByHeaderRules(rule, global);
        }
        else
        {
            return new(false);
        }
    }

    protected virtual RateLimitOptions MergeRateLimitByHeaderRules(FileRateLimitByHeaderRule rule, FileGlobalRateLimitByHeaderRule global)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(global);

        rule.ClientIdHeader = rule.ClientIdHeader.IfEmpty(global.ClientIdHeader.IfEmpty(RateLimitOptions.DefaultClientHeader));
        rule.ClientWhitelist ??= global.ClientWhitelist ?? [];

        // Final merging of EnableHeaders is implemented in the constructor
        rule.DisableRateLimitHeaders ??= global.DisableRateLimitHeaders;
        rule.EnableHeaders ??= global.EnableHeaders;

        rule.EnableRateLimiting ??= global.EnableRateLimiting ?? true;

        // Final merging of StatusCode is implemented in the constructor
        rule.HttpStatusCode ??= global.HttpStatusCode;
        rule.StatusCode ??= global.StatusCode;

        // Final merging of QuotaMessage is implemented in the constructor
        rule.QuotaExceededMessage = rule.QuotaExceededMessage.IfEmpty(global.QuotaExceededMessage);
        rule.QuotaMessage = rule.QuotaMessage.IfEmpty(global.QuotaMessage);

        // Final merging of KeyPrefix is implemented in the constructor
        rule.RateLimitCounterPrefix = rule.RateLimitCounterPrefix.IfEmpty(global.RateLimitCounterPrefix);
        rule.KeyPrefix = rule.KeyPrefix.IfEmpty(global.KeyPrefix);

        rule.Period = rule.Period.IfEmpty(global.Period.IfEmpty(RateLimitRule.DefaultPeriod));

        // Final merging of Wait is implemented in the constructor
        rule.PeriodTimespan ??= global.PeriodTimespan;
        rule.Wait = rule.Wait.IfEmpty(global.Wait.IfEmpty(RateLimitRule.ZeroWait));

        rule.Limit ??= global.Limit ?? RateLimitRule.ZeroLimit;
        return new(rule);
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
                EnableHeaders = globalRule.DisableRateLimitHeaders.HasValue ? !globalRule.DisableRateLimitHeaders.Value : globalRule.EnableHeaders ?? true,
                EnableRateLimiting = globalRule.EnableRateLimiting ?? true,
                StatusCode = globalRule.HttpStatusCode ?? globalRule.StatusCode ?? StatusCodes.Status429TooManyRequests,
                QuotaMessage = globalRule.QuotaExceededMessage.IfEmpty(globalRule.QuotaMessage),
                KeyPrefix = globalRule.RateLimitCounterPrefix.IfEmpty(globalRule.KeyPrefix),
                RateLimitRule = new(globalRule.Period,
                    globalRule.PeriodTimespan.HasValue ? $"{globalRule.PeriodTimespan.Value}s" : globalRule.Wait,
                    globalRule.Limit ?? RateLimitRule.ZeroLimit),
                ClientWhitelist = globalRule.ClientWhitelist ?? GlobalClientWhitelist(),
            };
        }

        return new() { EnableRateLimiting = false };
    }

    protected virtual List<string> GlobalClientWhitelist() => new();

    protected static string Empty(string str, string def) => str.IfEmpty(def);
}
