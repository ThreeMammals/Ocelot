using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;

namespace Ocelot.RateLimiting
{
    public class ClientRateLimitProcessor // TODO Interface extraction
    {
        private readonly RateLimitCore _core;

        public ClientRateLimitProcessor(IRateLimitCounterHandler counterHandler)
        {
            _core = new RateLimitCore(counterHandler);
        }

        public RateLimitCounter ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitOptions option)
            => _core.ProcessRequest(requestIdentity, option);

        public static int RetryAfterFrom(DateTime timestamp, RateLimitRule rule)
            => RateLimitCore.RetryAfterFrom(timestamp, rule);

        public RateLimitHeaders GetRateLimitHeaders(HttpContext context, ClientRequestIdentity requestIdentity, RateLimitOptions option)
            => _core.GetRateLimitHeaders(context, requestIdentity, option);

        public static TimeSpan ConvertToTimeSpan(string timeSpan)
            => RateLimitCore.ConvertToTimeSpan(timeSpan);
    }
}
