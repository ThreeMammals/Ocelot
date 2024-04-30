using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using System.Globalization;
using System.Security.Cryptography;

namespace Ocelot.RateLimiting;

public class RateLimitCore : IRateLimitCore
{
    private readonly IRateLimitStorage _storage;
    private static readonly object ProcessLocker = new();

    public RateLimitCore(IRateLimitStorage storage)
    {
        _storage = storage;
    }

    public virtual RateLimitCounter ProcessRequest(ClientRequestIdentity identity, RateLimitOptions options)
    {
        RateLimitCounter counter;
        var rule = options.RateLimitRule;
        var counterId = GetStorageKey(identity, options);

        // Serial reads and writes
        lock (ProcessLocker)
        {
            var entry = _storage.Get(counterId);
            counter = CountRequests(entry, rule);
        }

        TimeSpan expirationTime = ToTimespan(rule.Period);
        if (counter.TotalRequests > rule.Limit)
        {
            var retryAfter = RetryAfterFrom(counter.Timestamp, rule);
            if (retryAfter > 0)
            {
                // Rate Limit exceeded, ban period is active
                expirationTime = TimeSpan.FromSeconds(rule.PeriodTimespan); // TODO retryAfter seconds?
            }
            else
            {
                // Ban period elapsed, start counting
                _storage.Remove(counterId);
                counter = new RateLimitCounter(counter.Timestamp, 1);
            }
        }

        _storage.Set(counterId, counter, expirationTime);
        return counter;
    }

    protected virtual RateLimitCounter CountRequests(RateLimitCounter? entry, RateLimitRule rule)
    {
        if (!entry.HasValue) // no entry, start counting
        {
            return new RateLimitCounter(DateTime.UtcNow, 1); // current request is the 1st one
        }

        var counter = entry.Value;
        if (counter.Timestamp + ToTimespan(rule.Period) >= DateTime.UtcNow) // entry has not expired
        {
            var totalRequests = counter.TotalRequests + 1; // increment request count
            return new RateLimitCounter(counter.Timestamp, totalRequests); // deep copy
        }

        // Entry not expired, rate limit exceeded
        if (counter.TotalRequests > rule.Limit)
        {
            return counter;
        }

        // Rate limit not exceeded, period elapsed, start counting
        return new RateLimitCounter(DateTime.UtcNow, 1);
    }

    // TODO Should be protected actually
    public virtual void SaveCounter(ClientRequestIdentity identity, RateLimitOptions options, RateLimitCounter counter, TimeSpan expiration)
    {
        var counterId = GetStorageKey(identity, options);

        // Store with key: id (string) - timestamp (datetime) - total_requests (long)
        _storage.Set(counterId, counter, expiration);
    }

    public virtual RateLimitHeaders GetHeaders(HttpContext context, ClientRequestIdentity identity, RateLimitOptions options)
    {
        var rule = options.RateLimitRule;
        RateLimitHeaders headers;
        var counterId = GetStorageKey(identity, options);
        var entry = _storage.Get(counterId);
        if (entry.HasValue)
        {
            headers = new RateLimitHeaders(context,
                limit: rule.Period,
                remaining: (rule.Limit - entry.Value.TotalRequests).ToString(),
                reset: (entry.Value.Timestamp + ToTimespan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo));
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

    // TODO Should be protected actually
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

    // TODO Should be protected actually
    public virtual int RetryAfterFrom(DateTime startedAt, RateLimitRule rule)
    {
        var secondsPast = Convert.ToInt32((DateTime.UtcNow - startedAt).TotalSeconds);
        var retryAfter = Convert.ToInt32(TimeSpan.FromSeconds(rule.PeriodTimespan).TotalSeconds);
        retryAfter = retryAfter > 1 ? retryAfter - secondsPast : 1;
        return retryAfter;
    }

    /// <summary>
    /// Converts to time span from a string, such as "1s", "1m", "1h", "1d".
    /// </summary>
    /// <param name="timespan">The string value with dimentions: '1s', '1m', '1h', '1d'.</param>
    /// <returns>A <see cref="TimeSpan"/> value.</returns>
    /// <exception cref="FormatException">By default if the value dimension can't be detected.</exception>
    public virtual TimeSpan ToTimespan(string timespan)
    {
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
