using System;

namespace Ocelot.Configuration
{
    public class RateLimitRule
    {
        public RateLimitRule(long period, long limit)
        {
            Period = TimeSpan.FromMilliseconds(period);
            Limit = limit;
        }

        /// <summary>
        /// Rate limit period in milliseconds
        /// </summary>
        public TimeSpan Period { get; private set; }
        
        /// <summary>
        /// Maximum number of requests that a client can make in a defined period
        /// </summary>
        public long Limit { get; private set; }
    }
}
