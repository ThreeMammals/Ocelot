namespace Ocelot.Configuration.Builder;

public class QoSOptionsBuilder // TODO : QoSOptions LoL :D
{
    private int _exceptionsAllowedBeforeBreaking;
    private int _durationOfBreak;
    private double _failureRatio;
    private int _samplingDuration;
    private int? _timeoutValue;
    private string _key;

    public QoSOptionsBuilder WithExceptionsAllowedBeforeBreaking(int exceptionsAllowedBeforeBreaking)
    {
        _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
        return this;
    }

    public QoSOptionsBuilder WithDurationOfBreak(int durationOfBreak)
    {
        _durationOfBreak = durationOfBreak;
        return this;
    }

    public QoSOptionsBuilder WithTimeoutValue(int? timeoutValue)
    {
        _timeoutValue = timeoutValue;
        return this;
    }

    public QoSOptionsBuilder WithKey(string input)
    {
        _key = input;
        return this;
    }

    public QoSOptionsBuilder WithFailureRatio(double failureRatio)
    {
        _failureRatio = failureRatio;
        return this;
    }

    public QoSOptionsBuilder WithSamplingDuration(int samplingDuration)
    {
        _samplingDuration = samplingDuration;
        return this;
    }

    public QoSOptions Build() => new(_exceptionsAllowedBeforeBreaking, _durationOfBreak,
        timeoutValue: _timeoutValue,
        key: _key,
        failureRatio: _failureRatio,
        samplingDuration: _samplingDuration);
}
