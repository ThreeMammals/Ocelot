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
        /// Rate limit period as in 1s, 1m, 1h,1d
        /// </summary>
        public string Period { get; }

        public double PeriodTimespan { get; }

        /// <summary>
        /// Maximum number of requests that a client can make in a defined period
        /// </summary>
        public long Limit { get; }
    }
}
