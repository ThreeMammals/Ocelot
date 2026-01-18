using Ocelot.Configuration;

namespace Ocelot.Provider.Polly;

/// <summary>
/// Polly requirements for the <see href="https://www.pollydocs.org/strategies/circuit-breaker.html">Circuit breaker resilience strategy</see>.
/// The subjects of this strategy are the <see cref="QoSOptions.MinimumThroughput"/> and <see cref="QoSOptions.BreakDuration"/> properties.
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
    public static int BreakDuration(int milliseconds) => IsValidBreakDuration(milliseconds) ? milliseconds : DefaultBreakDuration;
    public static bool IsValidBreakDuration(this int milliseconds) => milliseconds > LowBreakDuration && milliseconds < HighBreakDuration;

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
    public static int MinimumThroughput(int failures) => IsValidMinimumThroughput(failures) ? failures : DefaultMinimumThroughput;
    public static bool IsValidMinimumThroughput(this int failures) => failures >= LowMinimumThroughput;

    // --- FailureRatio ---
    // Actual Polly's FailureRatio constraint -> https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_FailureRatio
    public const double LowFailureRatio = 0.0D; // ~ 0%
    public const double HighFailureRatio = 1.0D; // ~100%

    /// <summary>The FailureRatio default value is 0.1 (i.e. 10%).</summary>
    public const double DefaultFailureRatio = 0.1D; // ~10%

    /// <summary>
    /// Applies Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_FailureRatio">FailureRatio</see> constraint to the value.
    /// <para>If using Polly v8 or later, and in accordance with Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_FailureRatio">FailureRatio</see> constraint, this value must be greater than <see cref="LowFailureRatio"/> (0) and less than <see cref="HighFailureRatio"/> (1).</para></summary>
    /// <param name="ratio">The value as quotient (~ percents).</param>
    /// <returns>The same value if the constraint is satisfied; otherwise, the default value (<see cref="DefaultFailureRatio"/>).</returns>
    public static double FailureRatio(double ratio) => IsValidFailureRatio(ratio) ? ratio : DefaultFailureRatio;
    public static bool IsValidFailureRatio(this double ratio) => ratio > LowFailureRatio && ratio < HighFailureRatio;

    // --- SamplingDuration ---
    // Actual Polly's SamplingDuration constraint -> https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_SamplingDuration
    public const int LowSamplingDuration = 500; // 0.5 seconds
    public const int HighSamplingDuration = 86_400_000; // 1 day, 24 hours in milliseconds

    /// <summary>The SamplingDuration default value is 30 seconds, in milliseconds.</summary>
    public const int DefaultSamplingDuration = 30_000; // 30 seconds

    /// <summary>
    /// Applies Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_SamplingDuration">SamplingDuration</see> constraint to the value.
    /// <para>If using Polly v8 or later, and in accordance with Polly's <see href="https://www.pollydocs.org/api/Polly.CircuitBreaker.CircuitBreakerStrategyOptions-1.html#Polly_CircuitBreaker_CircuitBreakerStrategyOptions_1_SamplingDuration">SamplingDuration</see> constraint, this value must be greater than <see cref="LowSamplingDuration"/> (0.5 seconds) and less than <see cref="HighSamplingDuration"/> (1 day).</para></summary>
    /// <param name="milliseconds">The value in milliseconds.</param>
    /// <returns>The same value if the constraint is satisfied; otherwise, the default value (<see cref="DefaultSamplingDuration"/>).</returns>
    public static int SamplingDuration(int milliseconds) => IsValidSamplingDuration(milliseconds) ? milliseconds : DefaultSamplingDuration;
    public static bool IsValidSamplingDuration(this int milliseconds) => milliseconds > LowSamplingDuration && milliseconds < HighSamplingDuration;
}
