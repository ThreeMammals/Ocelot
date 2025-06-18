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

    /// <summary>Gets the value for <see href="https://www.pollydocs.org/strategies/timeout.html">Timeout resilience strategy</see> in milliseconds.
    /// <para>If using Polly v8 or later, and in accordance with Polly's <see href="https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout">Timeout</see> constraint, this value must be greater than <see cref="LowTimeout"/> (10 milliseconds) and less than <see cref="HighTimeout"/> (24 hours).</para></summary>
    /// <remarks>The default is <see cref="DefTimeout"/> (30 seconds), which can be overridden by the global <see cref="DefaultTimeout"/> property.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value (milliseconds).</value>
    public int? TimeoutValue { get; }

    // Actual Polly's Timeout constraint -> https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout
    public const int LowTimeout = 10; // 10 ms
    public const int DefTimeout = 30_000; // 30 seconds
    public const int HighTimeout = 86_400_000; // 24 hours in milliseconds

    /// <summary>
    /// Applies Polly's <see href="https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout">Timeout</see> constraint to the value.
    /// </summary>
    /// <param name="value">The value in milliseconds.</param>
    /// <returns>The same value if the constraint is satisfied; otherwise, the default value (<see cref="DefTimeout"/>).</returns>
    public static int ApplyTimeoutConstraint(int value) => (value > LowTimeout && value < HighTimeout) ? value : DefTimeout;

    /// <summary>Gets or sets the default timeout in milliseconds, which overrides Polly's default of 30 seconds.
    /// <para>The setter enforces Polly's <see href="https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout">Timeout</see> constraint that the assigned value must fall within the range (<see cref="LowTimeout"/>, <see cref="HighTimeout"/>).</para></summary>
    /// <remarks>By default, initialized to <see cref="DefTimeout"/> (30 seconds). </remarks>
    /// <value>An <see cref="int"/> value in milliseconds.</value>
    public static int DefaultTimeout { get => defaultTimeout; set => defaultTimeout = ApplyTimeoutConstraint(value); }
    private static int defaultTimeout = DefTimeout;

    public bool UseQos => ExceptionsAllowedBeforeBreaking > 0 || (TimeoutValue.HasValue && TimeoutValue > 0);
}
