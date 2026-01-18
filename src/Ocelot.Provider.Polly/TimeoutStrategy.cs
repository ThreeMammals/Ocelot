using Ocelot.Configuration;

namespace Ocelot.Provider.Polly;

/// <summary>
/// Polly requirements for the <see href="https://www.pollydocs.org/strategies/timeout.html">Timeout resilience strategy</see>.
/// The subject of this strategy is the <see cref="QoSOptions.Timeout"/> property.
/// </summary>
public static class TimeoutStrategy
{
    // Actual Polly's Timeout constraint -> https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout
    public const int LowTimeout = 10; // 10 ms
    public const int DefTimeout = 30_000; // 30 seconds
    public const int HighTimeout = 86_400_000; // 24 hours in milliseconds

    /// <summary>
    /// Applies Polly's <see href="https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout">Timeout</see> constraint to the value.
    /// <para>If using Polly v8 or later, and in accordance with Polly's <see href="https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout">Timeout</see> constraint, this value must be greater than <see cref="LowTimeout"/> (10 milliseconds) and less than <see cref="HighTimeout"/> (24 hours).</para></summary>
    /// <param name="milliseconds">The value in milliseconds.</param>
    /// <returns>The same value if the constraint is satisfied; otherwise, <see langword="null"/>.</returns>
    public static int? Timeout(int milliseconds) => IsValidTimeout(milliseconds) ? milliseconds : null;
    public static bool IsValidTimeout(this int milliseconds) => milliseconds > LowTimeout && milliseconds < HighTimeout;

    /// <summary>Gets or sets the default timeout in milliseconds, which overrides Polly's default of 30 seconds.
    /// <para>The setter enforces Polly's <see href="https://www.pollydocs.org/api/Polly.Timeout.TimeoutStrategyOptions.html#Polly_Timeout_TimeoutStrategyOptions_Timeout">Timeout</see> constraint that the assigned value must fall within the range (<see cref="LowTimeout"/>, <see cref="HighTimeout"/>).</para></summary>
    /// <remarks>By default, initialized to <see cref="DefTimeout"/> (30 seconds).</remarks>
    /// <value>An <see cref="int"/> value in milliseconds.</value>
    public static int DefaultTimeout { get => defaultTimeout; set => defaultTimeout = Timeout(value) ?? DefTimeout; }
    private static int defaultTimeout = DefTimeout;
}
