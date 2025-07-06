using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public class QoSOptions
{
    protected QoSOptions() { }

    /// <summary>Initializes a new instance of the <see cref="QoSOptions"/> class.</summary>
    /// <remarks>This is the copying constructor.</remarks>
    /// <param name="from">The object to copy the properties from.</param>
    public QoSOptions(QoSOptions from)
    {
        Key = from.Key;
        DurationOfBreak = from.DurationOfBreak;
        ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        TimeoutValue = from.TimeoutValue;
    }

    /// <summary>Initializes a new instance of the <see cref="QoSOptions"/> class from a <see cref="FileQoSOptions"/> model.</summary>
    /// <remarks>This is the converting constructor.</remarks>
    /// <param name="from">The File-model to copy the properties from.</param>
    public QoSOptions(FileQoSOptions from)
    {
        Key = string.Empty;
        DurationOfBreak = from.DurationOfBreak;
        ExceptionsAllowedBeforeBreaking = from.ExceptionsAllowedBeforeBreaking;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        TimeoutValue = from.TimeoutValue;
    }

    public QoSOptions(
        int exceptionsAllowedBeforeBreaking,
        int durationOfBreak,
        int? timeoutValue = null, 
        string key = null,
        double failureRatio = 0.0D,
        int samplingDuration = 0)
    {
        Key = key;
        DurationOfBreak = durationOfBreak;
        ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
        FailureRatio = failureRatio;
        SamplingDuration = samplingDuration;
        TimeoutValue = timeoutValue;
    }

    public string Key { get; protected set; }

    /// <summary>Gets the duration, in milliseconds, that the circuit remains open before resetting.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the CircuitBreakerStrategy class.</remarks>
    /// <value>An <see cref="int"/> value (milliseconds).</value>
    public int DurationOfBreak { get; protected set; }

    /// <summary>Gets the minimum number of failures required before the circuit is set to open.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the CircuitBreakerStrategy class.</remarks>
    /// <value>An <see cref="int"/> value (exceptions number).</value>
    public int ExceptionsAllowedBeforeBreaking { get; protected set; }

    /// <summary>
    /// The failure-success ratio that will cause the circuit to break/open. 
    /// </summary>
    /// <value>
    /// An <see cref="double"/> ratio of exceptions/requests  (0.8 means 80% failed of all sampled executions).
    /// </value>
    public double FailureRatio { get; protected set; }

    /// <summary>
    /// The time period over which the failure-success ratio is calculated (in milliseconds).
    /// </summary>
    /// <value>
    /// An <see cref="int"/> Time period in milliseconds.
    /// </value>
    public int SamplingDuration { get; protected set; }

    /// <summary>Gets the timeout in milliseconds.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the TimeoutStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (milliseconds).</value>
    public int? TimeoutValue { get; protected set; }

    public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || (TimeoutValue.HasValue && TimeoutValue > 0);
}
