using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class LoadBalancerOptionsCreator : ILoadBalancerOptionsCreator
{
    public LoadBalancerOptions Create(FileLoadBalancerOptions options)
        => new(options);

    public LoadBalancerOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.LoadBalancerOptions, globalConfiguration.LoadBalancerOptions);
    }

    public LoadBalancerOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.LoadBalancerOptions, globalConfiguration.LoadBalancerOptions);
    }

    protected virtual LoadBalancerOptions Create(IRouteGrouping grouping, FileLoadBalancerOptions options, FileGlobalLoadBalancerOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(grouping);
        var group = globalOptions;
        var isGlobal = group?.RouteKeys is null || // undefined section or array option -> is global
            group.RouteKeys.Count == 0 || // empty collection -> is global
            group.RouteKeys.Contains(grouping.Key); // this route is in the group

        if (options == null && globalOptions != null && isGlobal)
        {
            return new(globalOptions);
        }

        if (options != null && globalOptions == null)
        {
            return new(options);
        }
        else if (options != null && globalOptions != null && !isGlobal)
        {
            return new(options);
        }

        if (options != null && globalOptions != null && isGlobal)
        {
            return Merge(options, globalOptions);
        }

        return new();
    }

    protected virtual LoadBalancerOptions Merge(FileLoadBalancerOptions options, FileLoadBalancerOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(globalOptions);
        options.Type = options.Type.IfEmpty(globalOptions.Type);
        options.Key = options.Key.IfEmpty(globalOptions.Key);
        options.Expiry ??= globalOptions.Expiry;
        return new(options);
    }
}
