using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using System.Globalization;
using System.Security.Cryptography;

namespace Ocelot.RateLimit
{
    public class RateLimitCore
    {
        private readonly IRateLimitCounterHandler _counterHandler;
        private static readonly object ProcessLocker = new();

        public RateLimitCore(IRateLimitCounterHandler counterStore)
        {
            _counterHandler = counterStore;
        }

        public RateLimitCounter ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            RateLimitCounter counter;
            var rule = option.RateLimitRule;

            var counterId = ComputeCounterKey(requestIdentity, option);

            // serial reads and writes
            lock (ProcessLocker)
            {
                var entry = _counterHandler.Get(counterId);
                counter = CountRequests(entry, rule);
            }

            TimeSpan expirationTime = ConvertToTimeSpan(rule.Period);
            if (counter.TotalRequests > rule.Limit)
            {
                var retryAfter = RetryAfterFrom(counter.Timestamp, rule);
                if (retryAfter > 0)
                {
                    // rate limit exceeded, ban period is active
                    expirationTime = TimeSpan.FromSeconds(rule.PeriodTimespan);
                }
                else
                {
                    // ban period elapsed, start counting
                    _counterHandler.Remove(counterId);
                    counter = new RateLimitCounter(counter.Timestamp, 1);
                }
            }

            _counterHandler.Set(counterId, counter, expirationTime);

            return counter;
        }

        private static RateLimitCounter CountRequests(RateLimitCounter? entry, RateLimitRule rule)
        {
            // no entry - start counting
            if (!entry.HasValue)
            {
                return new RateLimitCounter(DateTime.UtcNow, 1);
            }
            
            // entry has not expired
            if (entry.Value.Timestamp + ConvertToTimeSpan(rule.Period) >= DateTime.UtcNow)
            {
                // increment request count
                var totalRequests = entry.Value.TotalRequests + 1;

                // deep copy
                return new RateLimitCounter(entry.Value.Timestamp, totalRequests);
            }
            
            // entry not expired, rate limit exceeded
            if (entry.Value.TotalRequests > rule.Limit)
            {
                return entry.Value;
            }

            // rate limit not exceeded, period elapsed, start counting
            return new RateLimitCounter(DateTime.UtcNow, 1);
        }

        public void SaveRateLimitCounter(ClientRequestIdentity requestIdentity, RateLimitOptions option, RateLimitCounter counter, TimeSpan expirationTime)
        {
            var counterId = ComputeCounterKey(requestIdentity, option);
            var rule = option.RateLimitRule;

            // stores: id (string) - timestamp (datetime) - total_requests (long)
            _counterHandler.Set(counterId, counter, expirationTime);
        }

        public RateLimitHeaders GetRateLimitHeaders(HttpContext context, ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            var rule = option.RateLimitRule;
            RateLimitHeaders headers;
            var counterId = ComputeCounterKey(requestIdentity, option);
            var entry = _counterHandler.Get(counterId);
            if (entry.HasValue)
            {
                headers = new RateLimitHeaders(context, rule.Period,
                    (rule.Limit - entry.Value.TotalRequests).ToString(),
                    (entry.Value.Timestamp + ConvertToTimeSpan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo)
                    );
            }
            else
            {
                headers = new RateLimitHeaders(context,
                    rule.Period,
                    rule.Limit.ToString(),
                    (DateTime.UtcNow + ConvertToTimeSpan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo));
            }

            return headers;
        }

        public static string ComputeCounterKey(ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            var key = $"{option.RateLimitCounterPrefix}_{requestIdentity.ClientId}_{option.RateLimitRule.Period}_{requestIdentity.HttpVerb}_{requestIdentity.Path}";

            var idBytes = Encoding.UTF8.GetBytes(key);

            byte[] hashBytes;

            using (var algorithm = SHA1.Create())
            {
                hashBytes = algorithm.ComputeHash(idBytes);
            }

            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public static int RetryAfterFrom(DateTime timestamp, RateLimitRule rule)
        {
            var secondsPast = Convert.ToInt32((DateTime.UtcNow - timestamp).TotalSeconds);
            var retryAfter = Convert.ToInt32(TimeSpan.FromSeconds(rule.PeriodTimespan).TotalSeconds);
            retryAfter = retryAfter > 1 ? retryAfter - secondsPast : 1;
            return retryAfter;
        }

        public static TimeSpan ConvertToTimeSpan(string timeSpan)
        {
            var l = timeSpan.Length - 1;
            var value = timeSpan.Substring(0, l);
            var type = timeSpan.Substring(l, 1);

            switch (type)
            {
                case "d":
                    return TimeSpan.FromDays(double.Parse(value));

                case "h":
                    return TimeSpan.FromHours(double.Parse(value));

                case "m":
                    return TimeSpan.FromMinutes(double.Parse(value));

                case "s":
                    return TimeSpan.FromSeconds(double.Parse(value));

                default:
                    throw new FormatException($"{timeSpan} can't be converted to TimeSpan, unknown type {type}");
            }
        }
    }
}
