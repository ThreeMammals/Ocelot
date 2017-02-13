using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public HttpContext Context { get; private set; }

        public string Limit { get; private set; }

        public string Remaining { get; private set; }

        public string Reset { get; private set; }
    }
}
