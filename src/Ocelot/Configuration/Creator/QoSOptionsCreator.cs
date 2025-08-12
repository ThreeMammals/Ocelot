using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class QoSOptionsCreator : IQoSOptionsCreator
{
    public QoSOptions Create(FileQoSOptions options) => new(options);

    public QoSOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        FileQoSOptions qos = route.QoSOptions, global = globalConfiguration.QoSOptions;
        qos.DurationOfBreak ??= global.DurationOfBreak;
        qos.ExceptionsAllowedBeforeBreaking ??= global.ExceptionsAllowedBeforeBreaking;
        qos.FailureRatio ??= global.FailureRatio;
        qos.SamplingDuration ??= global.SamplingDuration;
        qos.TimeoutValue ??= global.TimeoutValue;
        return new(qos);
    }
}
