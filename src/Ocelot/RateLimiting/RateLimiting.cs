using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using System.Globalization;
using System.Security.Cryptography;

namespace Ocelot.RateLimiting;

public class RateLimiting : IRateLimiting
{
    private readonly IRateLimitStorage _storage;
    private static readonly object ProcessLocker = new();

    public RateLimiting(IRateLimitStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Main entry point to process the current request and apply the limiting rule.
    /// </summary>
    /// <remarks>Warning! The method performs the storage operations which MUST BE thread safe.</remarks>
    /// <param name="identity">The representation of current request.</param>
    /// <param name="options">The current rate limiting options.</param>
    /// <returns>A <see cref="RateLimitCounter"/> value.</returns>
    public virtual RateLimitCounter ProcessRequest(ClientRequestIdentity identity, RateLimitOptions options)
    {
        RateLimitCounter counter;
        var rule = options.RateLimitRule;
        var counterId = GetStorageKey(identity, options);

        // Serial reads/writes from/to the storage which must be thread safe
        lock (ProcessLocker)
        {
            var entry = _storage.Get(counterId);
            counter = Count(entry, rule);
            var expiration = ToTimespan(rule.Period); // default expiration is set for the Period value
            if (counter.TotalRequests > rule.Limit)
            {
                var retryAfter = RetryAfter(counter, rule); // the calculation depends on the counter returned from CountRequests
                if (retryAfter > 0)
                {
                    // Rate Limit exceeded, ban period is active
                    expiration = TimeSpan.FromSeconds(rule.PeriodTimespan); // current state should expire in the storage after ban period
                }
                else
                {
                    // Ban period elapsed, start counting
                    _storage.Remove(counterId); // the store can delete the element on its own using an expiration mechanism, but let's force the element to be deleted
                    counter = new RateLimitCounter(DateTime.UtcNow, null, 1);
                }
            }

            _storage.Set(counterId, counter, expiration);
        }

        return counter;
    }

    /// <summary>
    /// Counts requests based on the current counter state and taking into account the limiting rule.
    /// </summary>
    /// <param name="entry">Old counter with starting moment inside.</param>
    /// <param name="rule">The limiting rule.</param>
    /// <returns>A <see cref="RateLimitCounter"/> value.</returns>
    public virtual RateLimitCounter Count(RateLimitCounter? entry, RateLimitRule rule)
    {
        var now = DateTime.UtcNow;
        if (!entry.HasValue)
        {
            // no entry, start counting
            return new RateLimitCounter(now, null, 1); // current request is the 1st one
        }

        var counter = entry.Value;
        var total = counter.TotalRequests + 1; // increment request count
        var startedAt = counter.StartedAt;

        // Counting Period is active
        if (startedAt + ToTimespan(rule.Period) >= now)
        {
            var exceededAt = total >= rule.Limit && !counter.ExceededAt.HasValue // current request number equals to the limit
                ? now // the exceeding moment is now, the next request will fail but the current one doesn't
                : counter.ExceededAt;
            return new RateLimitCounter(startedAt, exceededAt, total); // deep copy
        }

        var wasExceededAt = counter.ExceededAt;
        return wasExceededAt + TimeSpan.FromSeconds(rule.PeriodTimespan) >= now // ban PeriodTimespan is active
            ? new RateLimitCounter(startedAt, wasExceededAt, total) // still count
            : new RateLimitCounter(now, null, 1); // Ban PeriodTimespan elapsed, start counting NOW!
    }

    public virtual RateLimitHeaders GetHeaders(HttpContext context, ClientRequestIdentity identity, RateLimitOptions options)
    {
        RateLimitHeaders headers;
        RateLimitCounter? entry;
        lock (ProcessLocker)
        {
            var counterId = GetStorageKey(identity, options);
            entry = _storage.Get(counterId);
        }

        var rule = options.RateLimitRule;
        if (entry.HasValue)
        {
            headers = new RateLimitHeaders(context,
                limit: rule.Period,
                remaining: (rule.Limit - entry.Value.TotalRequests).ToString(),
                reset: (entry.Value.StartedAt + ToTimespan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo));
        }
        else
        {
            headers = new RateLimitHeaders(context,
                limit: rule.Period, // TODO Double check
                remaining: rule.Limit.ToString(), // TODO Double check
                reset: (DateTime.UtcNow + ToTimespan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo));
        }

        return headers;
    }

    public virtual string GetStorageKey(ClientRequestIdentity identity, RateLimitOptions options)
    {
        var key = $"{options.RateLimitCounterPrefix}_{identity.ClientId}_{options.RateLimitRule.Period}_{identity.HttpVerb}_{identity.Path}";
        var idBytes = Encoding.UTF8.GetBytes(key);

        byte[] hashBytes;
        using (var algorithm = SHA1.Create())
        {
            hashBytes = algorithm.ComputeHash(idBytes);
        }

        return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
    }

    /// <summary>
    /// Gets the seconds to wait for the next retry by starting moment and the rule.
    /// </summary>
    /// <remarks>The method must be called after the <see cref="Count(RateLimitCounter?, RateLimitRule)"/> one.</remarks>
    /// <param name="counter">The counter state.</param>
    /// <param name="rule">The current rule.</param>
    /// <returns>An <see cref="int"/> value of seconds.</returns>
    public virtual double RetryAfter(RateLimitCounter counter, RateLimitRule rule)
    {
        const double defaultSeconds = 1.0D; // one second
        var periodTimespan = rule.PeriodTimespan < defaultSeconds
            ? defaultSeconds // allow values which are greater or equal to 1 second
            : rule.PeriodTimespan; // good value
        var now = DateTime.UtcNow;

        // Counting Period is active
        if (counter.StartedAt + ToTimespan(rule.Period) >= now)
        {
            return counter.TotalRequests < rule.Limit
                ? 0.0D // happy path, no need to retry, current request is valid
                : counter.ExceededAt.HasValue
                    ? periodTimespan - (now - counter.ExceededAt.Value).TotalSeconds // minus seconds past
                    : periodTimespan; // exceeding not yet detected -> let's ban for whole period
        }

        // Limit exceeding was happen && ban PeriodTimespan is active
        if (counter.ExceededAt.HasValue && counter.ExceededAt + TimeSpan.FromSeconds(periodTimespan) >= now)
        {
            var startedAt = counter.ExceededAt.Value; // ban period was started at
            double secondsPast = (now - startedAt).TotalSeconds;
            double retryAfter = periodTimespan - secondsPast;
            return retryAfter; // it can be negative, which means the wait in PeriodTimespan seconds has ended
        }

        return 0.0D; // ban period elapsed, no need to retry, current request is valid
    }

    /// <summary>
    /// Converts to time span from a string, such as "1s", "1m", "1h", "1d".
    /// </summary>
    /// <param name="timespan">The string value with dimentions: '1s', '1m', '1h', '1d'.</param>
    /// <returns>A <see cref="TimeSpan"/> value.</returns>
    /// <exception cref="FormatException">By default if the value dimension can't be detected.</exception>
    public virtual TimeSpan ToTimespan(string timespan)
    {
        if (string.IsNullOrEmpty(timespan))
        {
            return TimeSpan.Zero;
        }

        var len = timespan.Length - 1;
        var value = timespan.Substring(0, len);
        var type = timespan.Substring(len, 1);

        return type switch
        {
            "d" => TimeSpan.FromDays(double.Parse(value)),
            "h" => TimeSpan.FromHours(double.Parse(value)),
            "m" => TimeSpan.FromMinutes(double.Parse(value)),
            "s" => TimeSpan.FromSeconds(double.Parse(value)),
            _ => throw new FormatException($"{timespan} can't be converted to TimeSpan, unknown type {type}"),
        };
    }
}
