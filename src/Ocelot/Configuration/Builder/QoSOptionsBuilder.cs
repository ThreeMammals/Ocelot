namespace Ocelot.Configuration.Builder;

public class QoSOptionsBuilder : QoSOptions
{
    public QoSOptionsBuilder WithExceptionsAllowedBeforeBreaking(int? value)
    {
        ExceptionsAllowedBeforeBreaking = value;
        return this;
    }

    public QoSOptionsBuilder WithDurationOfBreak(int? value)
    {
        DurationOfBreak = value;
        return this;
    }

    public QoSOptionsBuilder WithTimeoutValue(int? value)
    {
        TimeoutValue = value;
        return this;
    }

    public QoSOptionsBuilder WithFailureRatio(double? value)
    {
        FailureRatio = value;
        return this;
    }

    public QoSOptionsBuilder WithSamplingDuration(int? value)
    {
        SamplingDuration = value;
        return this;
    }

    public QoSOptions Build() => this;
}
