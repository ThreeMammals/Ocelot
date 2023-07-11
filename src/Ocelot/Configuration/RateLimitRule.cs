namespace Ocelot.Configuration
{
    public class RateLimitRule
    {
        public RateLimitRule(string period, double periodTimespan, long limit)
        {
            Period = period;
            PeriodTimespan = periodTimespan;
            Limit = limit;
        }

        /// <summary>
        /// Rate limit period as in 1s, 1m, 1h, 1d.
        /// </summary>
        /// <value>
        /// A string value with rate limit period.
        /// </value>
        public string Period { get; }

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
    }
}
