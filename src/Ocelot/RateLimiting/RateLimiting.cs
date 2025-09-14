using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Configuration;
using System.Security.Cryptography;

namespace Ocelot.RateLimiting;

public class RateLimiting : IRateLimiting
{
    private readonly IRateLimitStorage _storage;
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0330 // Prefer 'System.Threading.Lock'
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
    /// <param name="now">The processing moment.</param>
    /// <returns>A <see cref="RateLimitCounter"/> value.</returns>
    public virtual RateLimitCounter ProcessRequest(ClientRequestIdentity identity, RateLimitOptions options, DateTime now)
    {
        RateLimitCounter counter;
        var rule = options.Rule;
        var counterId = GetStorageKey(identity, options);

        // Serial reads/writes from/to the storage which must be thread safe
        lock (ProcessLocker)
        {
            var entry = _storage.Get(counterId);
            counter = Count(entry, rule, now);
            var expiration = rule.PeriodSpan; // default expiration is set for the Period value
            if (counter.Total > rule.Limit)
            {
                var retryAfter = RetryAfter(counter, rule, now); // the calculation depends on the counter returned from CountRequests
                if (retryAfter > 0)
                {
                    // Rate Limit exceeded, ban period is active
                    expiration = rule.WaitSpan; // current state should expire in the storage after ban period
                }
                else
                {
                    // Wait window period elapsed, start counting
                    _storage.Remove(counterId); // the store can delete the element on its own using an expiration mechanism, but let's force the element to be deleted
                    counter = new RateLimitCounter(now);
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
    /// <param name="now">The processing moment.</param>
    /// <returns>A <see cref="RateLimitCounter"/> value.</returns>
    public virtual RateLimitCounter Count(RateLimitCounter? entry, RateLimitRule rule, DateTime now)
    {
        if (!entry.HasValue)
        {
            return new RateLimitCounter(now); // no entry, start counting, and the current request is the 1st one
        }

        var counter = entry.Value;
        if (++counter.Total > rule.Limit && !counter.ExceededAt.HasValue) // current request exceeds the limit
        {
            counter.ExceededAt = now; // the exceeding moment is now, this request should fail
        }

        bool isInFixedWindow = counter.StartedAt + rule.PeriodSpan >= now; // the fixed window counting period
        bool isInWaitWindow = counter.ExceededAt.HasValue && counter.ExceededAt + rule.WaitSpan > now; // is greater than! Avoid including equality, treating the end of waiting as the starting point of the next fixed window
        if (isInFixedWindow || isInWaitWindow)
        {
            return counter; // still count
        }

        return new RateLimitCounter(now); // Wait window period elapsed, start counting NOW!
    }

    public virtual RateLimitHeaders GetHeaders(HttpContext context, RateLimitOptions options, DateTime now, RateLimitCounter counter)
    {
        var rule = options.Rule;
        return new RateLimitHeaders(context,
            limit: rule.Limit,
            remaining: rule.Limit - counter.Total,
            reset: counter.StartedAt + rule.PeriodSpan);
    }

    /// <summary>
    /// Gets the SHA1-hashed value of a unique key for caching, using the <see cref="IMemoryCache"/> service through the <see cref="IRateLimitStorage"/> service.
    /// </summary>
    /// <remarks>Notes:<list type="bullet">
    /// <item>The generated identity key includes the <see cref="RateLimitOptions.KeyPrefix"/> as a prefix to ensure it is recognized in distributed storage systems, like <see cref="IDistributedCache"/> services, aiding users in observing/managing cached objects.
    /// By default, each Ocelot instance employs its own <see cref="IMemoryCache"/> service, without synchronization across instances.</item>
    /// </list></remarks>
    /// <param name="identity">Specifies the client's identity.</param>
    /// <param name="options">Defines the current route rate-limiting options.</param>
    /// <returns>Returns a SHA1-hashed <see cref="string"/> object as the caching key.</returns>
    public virtual string GetStorageKey(ClientRequestIdentity identity, RateLimitOptions options)
    {
        var key = $"{options.KeyPrefix}_{identity}_{options.Rule}";
        var idBytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = SHA1.HashData(idBytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Gets the seconds to wait for the next retry by starting moment and the rule.
    /// </summary>
    /// <remarks>The method must be called after the <see cref="Count(RateLimitCounter?, RateLimitRule, DateTime)"/> one.</remarks>
    /// <param name="counter">The counter state.</param>
    /// <param name="rule">The current rule.</param>
    /// <param name="now">The processing moment.</param>
    /// <returns>An <see cref="int"/> value of seconds.</returns>
    public virtual double RetryAfter(RateLimitCounter counter, RateLimitRule rule, DateTime now)
    {
        var waitWindow = rule.WaitSpan < OneMillisecond
            ? OneMillisecond // allow values which are greater or equal to 1 millisecond -> 0.001 aka header value precision, in seconds
            : rule.WaitSpan; // good value

        // Counting Period is active
        if (counter.StartedAt + rule.PeriodSpan >= now)
        {
            return counter.Total < rule.Limit
                ? 0.0D // happy path, no need to retry, current request is valid
                : counter.ExceededAt.HasValue
                    ? waitWindow.TotalSeconds - (now - counter.ExceededAt.Value).TotalSeconds // minus seconds past
                    : waitWindow.TotalSeconds; // exceeding not yet detected -> let's ban for whole period
        }

        // Limit exceeding was happen && ban Wait is active
        if (counter.ExceededAt.HasValue && counter.ExceededAt + waitWindow >= now)
        {
            var startedAt = counter.ExceededAt.Value; // ban period was started at
            var timePast = now - startedAt;
            var retryAfter = waitWindow - timePast;
            return retryAfter.TotalSeconds; // it can be negative, which means the wait in PeriodTimespan seconds has ended
        }

        return 0.0D; // ban period elapsed, no need to retry, current request is valid
    }

    private static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1.0D);

    /// <summary>
    /// Converts to time span from a string, such as "1ms", "1s", "1m", "1h", "1d".
    /// </summary>
    /// <param name="timespan">The string value with units: '1ms', '1s', '1m', '1h', '1d'.</param>
    /// <returns>A <see cref="TimeSpan"/> value.</returns>
    /// <exception cref="FormatException">See more in the <see cref="RateLimitRule.ParseTimespan(string)"/> method docs.</exception>
    public virtual TimeSpan ToTimespan(string timespan) => RateLimitRule.ParseTimespan(timespan);
}
