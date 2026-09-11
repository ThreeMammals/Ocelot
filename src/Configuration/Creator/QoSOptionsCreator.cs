using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class QoSOptionsCreator : IQoSOptionsCreator
{
    public QoSOptions Create(FileQoSOptions options)
        => new(options ?? new());

    public QoSOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.QoSOptions, globalConfiguration.QoSOptions);
    }

    public QoSOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.QoSOptions, globalConfiguration.QoSOptions);
    }

    protected virtual QoSOptions Create(IRouteGrouping grouping, FileQoSOptions options, FileGlobalQoSOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(grouping);

        bool isGlobal = globalOptions?.RouteKeys is null // undefined section or array option -> is global
            || globalOptions.RouteKeys.Count == 0 // empty collection -> is global
            || globalOptions.RouteKeys.Contains(grouping.Key); // this route is in the group

        if (options == null && globalOptions != null && isGlobal)
        {
            return new(globalOptions);
        }

        if (options != null && globalOptions == null)
        {
            return new(options);
        }

        if (options != null && globalOptions != null)
        {
            return isGlobal ? Merge(options, globalOptions) : new(options);
        }

        return new();
    }

    protected virtual QoSOptions Merge(FileQoSOptions options, FileQoSOptions global)
    {
        options ??= new();
        global ??= new();
        options.DurationOfBreak ??= global.DurationOfBreak;
        options.BreakDuration ??= global.BreakDuration;
        options.ExceptionsAllowedBeforeBreaking ??= global.ExceptionsAllowedBeforeBreaking;
        options.MinimumThroughput ??= global.MinimumThroughput;
        options.FailureRatio ??= global.FailureRatio;
        options.SamplingDuration ??= global.SamplingDuration;
        options.TimeoutValue ??= global.TimeoutValue;
        options.Timeout ??= global.Timeout;
        return new(options);
    }
}
