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

    /// <summary>How long the circuit should stay open before resetting in milliseconds.</summary>
    /// <remarks>If using Polly version 8 or above, this value must be 500 (0.5 sec) or greater.</remarks>
    /// <value>An <see cref="int"/> value (milliseconds).</value>
    public int DurationOfBreak { get; } = DefaultBreakDuration;
    public const int LowBreakDuration = 500; // 0.5 seconds
    public const int DefaultBreakDuration = 5_000; // 5 seconds

    /// <summary>How many times a circuit can fail before being set to open.</summary>
    /// <remarks>If using Polly version 8 or above, this value must be 2 or greater.</remarks>
    /// <value>An <see cref="int"/> value (no of exceptions).</value>
    public int ExceptionsAllowedBeforeBreaking { get; }
    public const int LowMinimumThroughput = 2;
    public const int DefaultMinimumThroughput = 100;

    public string Key { get; }

    /// <summary>Gets the timeout in milliseconds.</summary>
    /// <remarks>Note: Read the appropriate documentation in the Ocelot.Provider.Polly project, which is the sole consumer of this property. See the TimeoutStrategy class.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (milliseconds).</value>
    public int? TimeoutValue { get; }

    public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || (TimeoutValue.HasValue && TimeoutValue > 0);
}
