using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public class QoSOptions
{
    public QoSOptions() { }
    public QoSOptions(int? timeout) => Timeout = timeout;
    public QoSOptions(int? exceptions, int? breakMs)
    {
        BreakDuration = breakMs;
        MinimumThroughput = exceptions;
    }

    /// <summary>Initializes a new instance of the <see cref="QoSOptions"/> class.</summary>
    /// <remarks>This is the copying constructor.</remarks>
    /// <param name="from">The object to copy the properties from.</param>
    public QoSOptions(QoSOptions from)
    {
        BreakDuration = from.BreakDuration;
        MinimumThroughput = from.MinimumThroughput;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        Timeout = from.Timeout;
    }

    /// <summary>Initializes a new instance of the <see cref="QoSOptions"/> class from a <see cref="FileQoSOptions"/> model.</summary>
    /// <remarks>This is the converting constructor.</remarks>
    /// <param name="from">The File-model to copy the properties from.</param>
    public QoSOptions(FileQoSOptions from)
    {
        BreakDuration = from.DurationOfBreak;
        MinimumThroughput = from.ExceptionsAllowedBeforeBreaking;
        FailureRatio = from.FailureRatio;
        SamplingDuration = from.SamplingDuration;
        Timeout = from.TimeoutValue;
    }

    /// <summary>Gets the duration, in milliseconds, that the circuit remains open before resetting.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the CircuitBreakerStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (milliseconds).</value>
    public int? BreakDuration { get; init; }

    /// <summary>Gets the minimum number of failures required before the circuit is set to open.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the CircuitBreakerStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (exceptions number).</value>
    public int? MinimumThroughput { get; init; }

    /// <summary>Gets or sets the failure-to-success ratio at which the circuit will break.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the CircuitBreakerStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="double"/>) value.</value>
    public double? FailureRatio { get; init; }

    /// <summary>Gets or sets the milliseconds duration of the sampling over which <see cref="FailureRatio"/> is assessed.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the TimeoutStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (milliseconds).</value>
    public int? SamplingDuration { get; init; }

    /// <summary>Gets the timeout in milliseconds.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the TimeoutStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (milliseconds).</value>
    public int? Timeout { get; init; }

    public bool UseQos => (MinimumThroughput.HasValue && MinimumThroughput > 0)
        || (Timeout.HasValue && Timeout > 0);
}
