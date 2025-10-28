using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class RateLimitOptionsCreator : IRateLimitOptionsCreator
{
    public RateLimitOptionsCreator() { }

    public RateLimitOptions Create(FileGlobalConfiguration globalConfiguration)
        => globalConfiguration.RateLimitOptions != null
            ? new(globalConfiguration.RateLimitOptions)
            : new(false);

    public RateLimitOptions Create(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);

        var rule = route.RateLimitOptions;
        var globalOptions = globalConfiguration.RateLimitOptions;
        var group = globalOptions as IRouteGroup;

        // bool isGlobal = globalOptions?.RouteKeys?.Contains(route.Key) ?? true;
        bool isGlobal = group?.RouteKeys is null || // undefined section or array option -> is global
            group.RouteKeys.Count == 0 || // empty collection -> is global
            group.RouteKeys.Contains(route.Key); // this route is in the group

        if (rule?.EnableRateLimiting == false || (isGlobal && globalOptions?.EnableRateLimiting == false))
        {
            return new(false);
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

    protected virtual RateLimitOptions MergeHeaderRules(FileRateLimitByHeaderRule rule, FileRateLimitByHeaderRule globalRule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(globalRule);

        rule.ClientIdHeader = rule.ClientIdHeader.IfEmpty(globalRule.ClientIdHeader.IfEmpty(RateLimitOptions.DefaultClientHeader));
        rule.ClientWhitelist ??= globalRule.ClientWhitelist ?? [];

        // Final merging of EnableHeaders is implemented in the constructor
        rule.DisableRateLimitHeaders ??= globalRule.DisableRateLimitHeaders;
        rule.EnableHeaders ??= globalRule.EnableHeaders;

        rule.EnableRateLimiting ??= globalRule.EnableRateLimiting ?? true;

        // Final merging of StatusCode is implemented in the constructor
        rule.HttpStatusCode ??= globalRule.HttpStatusCode;
        rule.StatusCode ??= globalRule.StatusCode;

        // Final merging of QuotaMessage is implemented in the constructor
        rule.QuotaExceededMessage = rule.QuotaExceededMessage.IfEmpty(globalRule.QuotaExceededMessage);
        rule.QuotaMessage = rule.QuotaMessage.IfEmpty(globalRule.QuotaMessage);

        // Final merging of KeyPrefix is implemented in the constructor
        rule.RateLimitCounterPrefix = rule.RateLimitCounterPrefix.IfEmpty(globalRule.RateLimitCounterPrefix);
        rule.KeyPrefix = rule.KeyPrefix.IfEmpty(globalRule.KeyPrefix);

        rule.Period = rule.Period.IfEmpty(globalRule.Period.IfEmpty(RateLimitRule.DefaultPeriod));

        // Final merging of Wait is implemented in the constructor
        rule.PeriodTimespan ??= globalRule.PeriodTimespan;
        rule.Wait = rule.Wait.IfEmpty(globalRule.Wait.IfEmpty(RateLimitRule.ZeroWait));

        rule.Limit ??= globalRule.Limit ?? RateLimitRule.ZeroLimit;
        return new(rule);
    }
}
