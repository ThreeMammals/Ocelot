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
        var globalOptions = globalConfiguration.RateLimitOptions;

        // bool isGlobal = globalOptions?.RouteKeys?.Contains(route.Key) ?? true;
        bool isGlobal = globalOptions?.RouteKeys is null || // undefined section or array option -> is global
            globalOptions.RouteKeys.Count == 0 || // empty collection -> is global
            globalOptions.RouteKeys.Contains(route.Key); // this route is in the group

        if (rule?.EnableRateLimiting == false || (isGlobal && globalOptions?.EnableRateLimiting == false))
        {
            return new(false);
        }

        if (globalConfiguration.RateLimiting?.ByMethod != null)
        {
            return CreateMethodRules(route, globalConfiguration);
        }

        // By Client's Header rule merging
        if (rule == null && globalOptions != null && isGlobal)
        {
            return new(globalOptions);
        }

        if (rule != null && (globalOptions == null || (globalOptions != null && !isGlobal)))
        {
            return new(rule);
        }

        if (rule != null && globalOptions != null && isGlobal)
        {
            return MergeHeaderRules(rule, globalOptions);
        }

        return new(false);
    }

    protected virtual RateLimitOptions MergeHeaderRules(FileRateLimitByHeaderRule rule, FileGlobalRateLimitByHeaderRule global)
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

    public RateLimitOptions CreateMethodRules(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);

        var path = route.UpstreamPathTemplate ?? string.Empty;
        var globalRule = globalConfiguration.RateLimiting?.ByMethod
            .FirstOrDefault(rule => Regex.IsMatch(path, '^' + Regex.Escape(rule.Pattern).Replace("\\*", ".*") + '$', RegexOptions.IgnoreCase | RegexOptions.Compiled));
        if (globalRule != null)
        {
            return new RateLimitOptions()
            {
                ClientWhitelist = /*globalRule.ClientWhitelist ??*/ GlobalClientWhitelist(),
                EnableHeaders = globalRule.EnableHeaders ?? true,
                EnableRateLimiting = globalRule.EnableRateLimiting ?? true,
                StatusCode = globalRule.StatusCode ?? StatusCodes.Status429TooManyRequests,
                QuotaMessage = globalRule.QuotaMessage,
                KeyPrefix = globalRule.KeyPrefix,
                Rule = new(globalRule.Period,
                    globalRule.PeriodTimespan.HasValue ? $"{globalRule.PeriodTimespan.Value}s" : globalRule.Wait,
                    globalRule.Limit ?? RateLimitRule.ZeroLimit),
            };
        }

        return new() { EnableRateLimiting = false };
    }

    protected virtual List<string> GlobalClientWhitelist() => new();
}
