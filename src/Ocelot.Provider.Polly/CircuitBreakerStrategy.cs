using Ocelot.Configuration;

namespace Ocelot.Provider.Polly;

/// <summary>
/// Polly requirements for the <see href="https://www.pollydocs.org/strategies/circuit-breaker.html">Circuit breaker resilience strategy</see>.
/// The subjects of this strategy are the <see cref="QoSOptions.ExceptionsAllowedBeforeBreaking"/> and <see cref="QoSOptions.DurationOfBreak"/> properties.
/// </summary>
public static class CircuitBreakerStrategy
{
    // --- BreakDuration ---
    // Actual Polly's BreakDuration constraint -> https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_BreakDuration
    public const int LowBreakDuration = 500; // 0.5 seconds
    public const int HighBreakDuration = 86_400_000; // 1 day, 24 hours in milliseconds

    /// <summary>Default duration of break the circuit will stay open before resetting, in milliseconds.</summary>
    public const int DefaultBreakDuration = 5_000; // 5 seconds

    /// <summary>
    /// Applies Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_BreakDuration">BreakDuration</see> constraint to the value.
    /// <para>If using Polly v8 or later, and in accordance with Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_BreakDuration">BreakDuration</see> constraint, this value must be greater than <see cref="LowBreakDuration"/> (0.5 seconds) and less than <see cref="HighBreakDuration"/> (1 day).</para></summary>
    /// <param name="milliseconds">The value in milliseconds.</param>
    /// <returns>The same value if the constraint is satisfied; otherwise, the default value (<see cref="DefaultBreakDuration"/>).</returns>
    public static int BreakDuration(int milliseconds) => (milliseconds > LowBreakDuration && milliseconds < HighBreakDuration) ? milliseconds : DefaultBreakDuration;

    // --- MinimumThroughput ---
    // Actual Polly's MinimumThroughput constraint -> https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_MinimumThroughput
    public const int LowMinimumThroughput = 2;

    /// <summary>Default minimum throughput: this many actions or more must pass through the circuit in the time-slice, for statistics to be considered significant and the circuit-breaker to come into action.</summary>
    public const int DefaultMinimumThroughput = 100;

    /// <summary>
    /// Applies Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_MinimumThroughput">MinimumThroughput</see> constraint to the value.
    /// <para>If using Polly v8 or later, and in accordance with Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_MinimumThroughput">MinimumThroughput</see> constraint, this value must be <see cref="LowMinimumThroughput"/> (2) or greater.</para></summary>
    /// <param name="failures">The number of failures.</param>
    /// <returns>The same value if the constraint is satisfied; otherwise, the default value (<see cref="DefaultMinimumThroughput"/>).</returns>
    public static int MinimumThroughput(int failures) => (failures >= LowMinimumThroughput) ? failures : DefaultMinimumThroughput;
}
