using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class CacheOptionsCreator : ICacheOptionsCreator
{
    public CacheOptions Create(FileCacheOptions options)
        => new(options?.TtlSeconds, options?.Region, options?.Header, options?.EnableContentHashing);

    public CacheOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration, string loadBalancingKey)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.FileCacheOptions ?? route.CacheOptions, globalConfiguration.CacheOptions, loadBalancingKey);
    }

    public CacheOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration, string loadBalancingKey)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.CacheOptions, globalConfiguration.CacheOptions, loadBalancingKey);
    }

    protected virtual CacheOptions Create(IRouteGrouping grouping, FileCacheOptions options, FileGlobalCacheOptions globalOptions, string loadBalancingKey)
    {
        ArgumentNullException.ThrowIfNull(grouping);
        var group = globalOptions;
        bool isGlobal = group?.RouteKeys is null || // undefined section or array option -> is global
            group.RouteKeys.Count == 0 || // empty collection -> is global
            group.RouteKeys.Contains(grouping.Key); // this route is in the group

        if (options == null && globalOptions != null && isGlobal)
        {
            return new(globalOptions, loadBalancingKey);
        }

        if (options != null && globalOptions == null)
        {
            return new(options, loadBalancingKey);
        }
        else if (options != null && globalOptions != null && !isGlobal)
        {
            return new(options, loadBalancingKey);
        }

        if (options != null && globalOptions != null && isGlobal)
        {
            return Merge(options, globalOptions, loadBalancingKey);
        }

        return new();
    }

    protected virtual CacheOptions Merge(FileCacheOptions options, FileCacheOptions globalOptions, string defaultRegion)
    {
        var region = options.Region.IfEmpty(globalOptions.Region).IfEmpty(defaultRegion);
        var header = options.Header.IfEmpty(globalOptions.Header);
        var ttlSeconds = options.TtlSeconds ?? globalOptions.TtlSeconds;
        var enableHashing = options.EnableContentHashing ?? globalOptions.EnableContentHashing;
        return new CacheOptions(ttlSeconds, region, header, enableHashing);
    }
}
