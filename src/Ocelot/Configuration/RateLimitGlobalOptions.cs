using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration
{
    public class RateLimitGlobalOptions
    {
        public RateLimitGlobalOptions(string clientIdHeader, bool disableRateLimitHeaders,
            string quotaExceededMessage, string rateLimitCounterPrefix, int httpStatusCode)
        {
            ClientIdHeader = clientIdHeader;
            DisableRateLimitHeaders = disableRateLimitHeaders;
            QuotaExceededMessage = quotaExceededMessage;
            RateLimitCounterPrefix = rateLimitCounterPrefix;
            HttpStatusCode = httpStatusCode;
        }

        public string ClientIdHeader { get; private set; }
        public string QuotaExceededMessage { get; private set; }
        public string RateLimitCounterPrefix { get; private set; }
        public bool DisableRateLimitHeaders { get; private set; }
        public int HttpStatusCode { get; private set; }
    }
}
