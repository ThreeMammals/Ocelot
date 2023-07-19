using Microsoft.AspNetCore.Http;

namespace Ocelot.RateLimit
{
    public class RateLimitHeaders
    {
        public RateLimitHeaders(HttpContext context, string limit, string remaining, string reset)
        {
            Context = context;
            Limit = limit;
            Remaining = remaining;
            Reset = reset;
        }

        public HttpContext Context { get; }

        public string Limit { get; }

        public string Remaining { get; }

        public string Reset { get; }
    }
}
