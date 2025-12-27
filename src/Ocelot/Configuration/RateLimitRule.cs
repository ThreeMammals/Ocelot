using Ocelot.Infrastructure.Extensions;
using System.Globalization;

namespace Ocelot.Configuration;

public class RateLimitRule
{
    public const string DefaultPeriod = "1s";
    public const string ZeroWait = "0ms";
    public const long ZeroLimit = 0L;
    public static RateLimitRule Empty = new(DefaultPeriod, ZeroWait, ZeroLimit);

    public RateLimitRule(string period, string wait, long limit)
    {
        Period = period.IfEmpty(DefaultPeriod);
        Wait = wait.IfEmpty(ZeroWait);
        Limit = Math.Abs(limit);
    }

    public override string ToString() => $"{Limit}/{Period}/w{Wait}";

    /// <summary>
    /// Rate limiting durations can be set using units like 'ms' (milliseconds), 's' (seconds), 'm' (minutes), 'h' (hours), or 'd' (days).
    /// </summary>
    /// <value>A <see cref="string"/> object with the period (fixed window).</value>
    public string Period { get; }

    /// <summary>A processed form of the <see cref="Period"/> property optimized for quick algorithm computations.</summary>
    /// <value>A <see cref="TimeSpan"/> value.</value>
    public TimeSpan PeriodSpan { get => _periodSpan ??= ParseTimespan(Period); }
    private TimeSpan? _periodSpan;

    /// <summary>
    /// Wait window after exceeding the rate limit, which has 'ms', 's', 'm', 'h', 'd' units.
    /// </summary>
    /// <value>A <see cref="string"/> object with the waiting window.</value>
    public string Wait { get; }

    /// <summary>A processed form of the <see cref="Wait"/> property optimized for quick algorithm computations.</summary>
    /// <value>A <see cref="TimeSpan"/> value.</value>
    public TimeSpan WaitSpan { get => _waitSpan ??= ParseTimespan(Wait); }
    private TimeSpan? _waitSpan;

    /// <summary>
    /// Maximum number of requests that a client can make in a defined period.
    /// </summary>
    /// <value>A <see cref="long"/> value with maximum number of requests.</value>
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
            // TODO: Make sense to have validation in src/Ocelot/Configuration/Validator/RouteFluentValidator
            throw new FormatException($"The '{timespan}' value doesn't include any digits, so it cannot be considered a number!");
        }

        string val = timespan.Trim();
        int pos = val.Length;
        while (--pos >= 0 && !char.IsDigit(val[pos]) && val[pos] != DecimalSeparator)
        {
        }

        string floating = val[..++pos], unit = val[pos..];
        double value = Math.Abs(double.Parse(floating)); // negative values should be disallowed as they could cause everything to malfunction; TODO: Make sense to have validation in src/Ocelot/Configuration/Validator/RouteFluentValidator
        return unit.ToLower() switch
        {
            "d" => TimeSpan.FromDays(value),
            "h" => TimeSpan.FromHours(value),
            "m" => TimeSpan.FromMinutes(value),
            "s" => TimeSpan.FromSeconds(value),
            "ms" => TimeSpan.FromMilliseconds(value),
            "" => TimeSpan.FromMilliseconds(value), // an unknown unit defaults to milliseconds as the ms unit
            _ => throw new FormatException($"The '{timespan}' timespan cannot be converted to {nameof(TimeSpan)} due to an unknown '{unit}' unit!"),
        };
    }

    private static readonly char DecimalSeparator = new NumberFormatInfo().NumberDecimalSeparator[0];
}
