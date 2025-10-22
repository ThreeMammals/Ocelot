using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class QoSOptionsCreator : IQoSOptionsCreator
{
    public QoSOptions Create(FileQoSOptions options)
        => new(options);
    public QoSOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
        => Merge(route.QoSOptions, globalConfiguration.QoSOptions);
    public QoSOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration)
        => Merge(route.QoSOptions, globalConfiguration.QoSOptions);

    protected virtual QoSOptions Merge(FileQoSOptions options, FileQoSOptions global)
    {
        options ??= new();
        options.DurationOfBreak ??= global.DurationOfBreak;
        options.ExceptionsAllowedBeforeBreaking ??= global.ExceptionsAllowedBeforeBreaking;
        options.FailureRatio ??= global.FailureRatio;
        options.SamplingDuration ??= global.SamplingDuration;
        options.TimeoutValue ??= global.TimeoutValue;
        return new(options);
    }
}
