using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.RateLimit
{
    public class RateLimitCore
    {
        private readonly IRateLimitCounterHandler _counterHandler;
        private static readonly object _processLocker = new object();

        public RateLimitCore(IRateLimitCounterHandler counterStore)
        {
            _counterHandler = counterStore;
        }

        public RateLimitCounter ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            var rateLimitCounter = new RateLimitCounter(DateTime.UtcNow, 1);

            var rateLimitRule = option.RateLimitRule;

            var rateLimitCounterKey = ComputeCounterKey(requestIdentity, option);

            // serial reads and writes
            lock (_processLocker)
            {
                var existingRateLimitCounter = _counterHandler.Get(rateLimitCounterKey);

                if (existingRateLimitCounter.HasValue)
                {
                    // entry has not expired
                    if (EntryHasNotExpired(existingRateLimitCounter.Value, rateLimitRule))
                    {
                        // increment request count
                        var totalRequests = existingRateLimitCounter.Value.TotalRequests + 1;
                        
                        // deep copy
                        rateLimitCounter = new RateLimitCounter(existingRateLimitCounter.Value.Timestamp, totalRequests);
                    }
                }
            }

            if (rateLimitCounter.TotalRequests > rateLimitRule.Limit)
            {
                var retryAfter = RetryAfter(rateLimitCounter, rateLimitRule);

                if (retryAfter > 0)
                {
                    _counterHandler.Set(rateLimitCounterKey, rateLimitCounter, rateLimitRule.Period);
                }
                else
                {
                    _counterHandler.Remove(rateLimitCounterKey);
                }
            }
            else
            {
                _counterHandler.Set(rateLimitCounterKey, rateLimitCounter, rateLimitRule.Period);
            }

            return rateLimitCounter;
        }

        private bool EntryHasNotExpired(RateLimitCounter existingRateLimitCounter, RateLimitRule rateLimitRule)
        {
            return existingRateLimitCounter.Timestamp + rateLimitRule.Period >= DateTime.UtcNow;
        }

        public RateLimitHeaders GetRateLimitHeaders(HttpContext context, ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            var rule = option.RateLimitRule;
            RateLimitHeaders headers = null;
            var counterId = ComputeCounterKey(requestIdentity, option);
            var entry = _counterHandler.Get(counterId);
            if (entry.HasValue)
            {
                headers = new RateLimitHeaders(context, rule.Period,
                    (rule.Limit - entry.Value.TotalRequests).ToString(),
                    (entry.Value.Timestamp + rule.Period).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo)
                    );
            }
            else
            {
                headers = new RateLimitHeaders(context,
                    rule.Period,
                    rule.Limit.ToString(),
                    (DateTime.UtcNow + rule.Period).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo));
            }

            return headers;
        }

        public string ComputeCounterKey(ClientRequestIdentity requestIdentity, RateLimitOptions option)
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

        public int RetryAfter(RateLimitCounter counter, RateLimitRule rule)
        {
            var secondsPastBetweenNowAndTimestamp = Convert.ToInt32((DateTime.UtcNow - counter.Timestamp).TotalSeconds);
            // use of PeriodTimespan here is just converting its value to seconds, could it not just use the value as an int?
            var retryAfterPeriodTimespan = rule.Period.TotalSeconds;
            // if retryAfterPeriodTimespan is greater than one then return retryAfterPeriodTimespan minus secondsPastBetweenNowAndTimestamp else just return 1...
            retryAfterPeriodTimespan = retryAfterPeriodTimespan > 1 ? retryAfterPeriodTimespan - secondsPastBetweenNowAndTimestamp : 1;
            return (int)retryAfterPeriodTimespan;
        }
    }
}
