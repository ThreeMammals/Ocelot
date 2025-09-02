using System.Globalization;

namespace Ocelot.Configuration;

public class RateLimitRule
{
    public const string DefaultPeriod = "1s";
    public const double ZeroPeriodTimespan = 0.0D;
    public const long ZeroLimit = 0L;
    public static RateLimitRule Empty = new(DefaultPeriod, ZeroPeriodTimespan, ZeroLimit);

    public RateLimitRule(string period, double periodTimespan, long limit)
    {
        Period = string.IsNullOrWhiteSpace(period) ? DefaultPeriod : period;
        PeriodTimespan = Math.Abs(periodTimespan);
        Limit = Math.Abs(limit);
    }

    public override string ToString() => $"{Limit}/{Period}/w{PeriodTimespan:F}s";

    /// <summary>
    /// Rate limit durations can be set using units like '1ms' (1 millisecond), '1s' (1 second), '1m' (1 minute), '1h' (1 hour), or '1d (1 day).
    /// </summary>
    /// <value>A <see cref="string"/> object with the period (fixed window).</value>
    public string Period { get; }

    /// <summary>A processed form of the <see cref="Period"/> property optimized for quick algorithm computations.</summary>
    /// <value>A <see cref="TimeSpan"/> value.</value>
    public TimeSpan PeriodSpan { get => _periodSpan ??= ParseTimespan(Period); }
    private TimeSpan? _periodSpan;

    /// <summary>
    /// Timespan to wait after reaching the rate limit, in seconds.
    /// </summary>
    /// <value>
    /// A double floating-point integer with timespan, in seconds.
    /// </value>
    public double PeriodTimespan { get; }

    /// <summary>
    /// Maximum number of requests that a client can make in a defined period.
    /// </summary>
    /// <value>
    /// A long integer with maximum number of requests.
    /// </value>
    public long Limit { get; }

    /// <summary>
    /// Parses a timespan string, such as "1ms", "1s", "1m", "1h", "1d".
    /// </summary>
    /// <remarks>Converts a string to milliseconds when the unit is missing or undefined, automatically applying the 'ms' unit.</remarks>
    /// <param name="timespan">The string value with units: '1ms', '1s', '1m', '1h', '1d'.</param>
    /// <returns>A <see cref="TimeSpan"/> value.</returns>
    /// <exception cref="FormatException">If the value is not a number, or the unit of value cannot be determined.</exception>
    public static TimeSpan ParseTimespan(string timespan)
    {
        if (string.IsNullOrWhiteSpace(timespan))
        {
            return TimeSpan.Zero;
        }

        if (!timespan.Any(char.IsDigit))
        {
            throw new FormatException($"The '{timespan}' value doesn't include any digits, so it cannot be considered a number!");
        }

        string val = timespan.Trim();
        int pos = val.Length;
        while (--pos >= 0 && !char.IsDigit(val[pos]) && val[pos] != DecimalSeparator)
        {
        }

        string floating = val[..++pos], unit = val[pos..];
        double value = Math.Abs(double.Parse(floating)); // negative values should be disallowed as they could cause everything to malfunction
        return unit switch
        {
            "d" => TimeSpan.FromDays(value),
            "h" => TimeSpan.FromHours(value),
            "m" => TimeSpan.FromMinutes(value),
            "s" => TimeSpan.FromSeconds(value),
            "ms" => TimeSpan.FromMilliseconds(value),
            _ when string.IsNullOrEmpty(unit) => TimeSpan.FromMilliseconds(value),
            _ => throw new FormatException($"The '{timespan}' timespan cannot be converted to {nameof(TimeSpan)} due to an unknown '{unit}' unit!"),
        };
    }

    private static readonly char DecimalSeparator = new NumberFormatInfo().NumberDecimalSeparator[0];
}
