using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public class QoSOptions
{
    public QoSOptions(QoSOptions from)
    {
        DurationOfBreak = from.DurationOfBreak;
        ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
        Key = from.Key;
        TimeoutValue = from.TimeoutValue;
    }

    public QoSOptions(FileQoSOptions from)
    {
        DurationOfBreak = from.DurationOfBreak;
        ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
        Key = string.Empty;
        TimeoutValue = from.TimeoutValue;
    }

    public QoSOptions(
        int exceptionsAllowedBeforeBreaking,
        int durationOfBreak,
        int? timeoutValue, 
        string key)
    {
        DurationOfBreak = durationOfBreak;
        ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
        Key = key;
        TimeoutValue = timeoutValue;
    }

    public QoSOptions(
        int exceptionsAllowedBeforeBreaking,
        int durationOfBreak,
        double failureRatio,
        int timeoutValue,
        string key)
    {
        DurationOfBreak = durationOfBreak;
        ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
        Key = key;
        TimeoutValue = timeoutValue;
        FailureRatio = failureRatio;
    }

    public QoSOptions(
        int exceptionsAllowedBeforeBreaking,
        int durationOfBreak,
        double failureRatio,
        int samplingDuration,
        int timeoutValue,
        string key)
    {
        DurationOfBreak = durationOfBreak;
        ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
        Key = key;
        TimeoutValue = timeoutValue;
        FailureRatio = failureRatio;
        SamplingDuration = samplingDuration;
    }

    /// <summary>Gets the duration, in milliseconds, that the circuit remains open before resetting.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the CircuitBreakerStrategy class.</remarks>
    /// <value>An <see cref="int"/> value (milliseconds).</value>
    public int DurationOfBreak { get; }

    /// <summary>Gets the minimum number of failures required before the circuit is set to open.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the CircuitBreakerStrategy class.</remarks>
    /// <value>An <see cref="int"/> value (exceptions number).</value>
    public int ExceptionsAllowedBeforeBreaking { get; }

    public string Key { get; }

    /// <summary>
    /// The failure-success ratio that will cause the circuit to break/open. 
    /// </summary>
    /// <value>
    /// An <see cref="double"/> ratio of exceptions/requests  (0.8 means 80% failed of all sampled executions).
    /// </value>
    public double FailureRatio { get; } = .8;

    /// <summary>
    /// The time period over which the failure-success ratio is calculated (in milliseconds).
    /// </summary>
    /// <value>
    /// An <see cref="int"/> Time period in milliseconds.
    /// </value>
    public int SamplingDuration { get; } = 10000;

    /// <summary>Gets the timeout in milliseconds.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the TimeoutStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (milliseconds).</value>
    public int? TimeoutValue { get; }

    public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || (TimeoutValue.HasValue && TimeoutValue > 0);

    public bool IsValid() =>
        ExceptionsAllowedBeforeBreaking <= 0 ||
        ExceptionsAllowedBeforeBreaking >= 2 && DurationOfBreak > 0 && !(FailureRatio <= 0) &&
        !(FailureRatio > 1) && SamplingDuration > 0;
}
