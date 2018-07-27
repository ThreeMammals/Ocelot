using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.Builder
{
    public class RateLimitGlobalOptionsBuilder
    {
        private string _clientIdHeader;
        private bool _disableRateLimitHeaders;
        private string _quotaExceededMessage;
        private string _rateLimitCounterPrefix;
        private int _httpStatusCode;
        
        public RateLimitGlobalOptionsBuilder WithClientIdHeader(string clientIdheader)
        {
            _clientIdHeader = clientIdheader;
            return this;
        }

        public RateLimitGlobalOptionsBuilder WithDisableRateLimitHeaders(bool disableRateLimitHeaders)
        {
            _disableRateLimitHeaders = disableRateLimitHeaders;
            return this;
        }

        public RateLimitGlobalOptionsBuilder WithQuotaExceededMessage(string quotaExceededMessage)
        {
            _quotaExceededMessage = quotaExceededMessage;
            return this;
        }

        public RateLimitGlobalOptionsBuilder WithRateLimitCounterPrefix(string rateLimitCounterPrefix)
        {
            _rateLimitCounterPrefix = rateLimitCounterPrefix;
            return this;
        }
        
        public RateLimitGlobalOptionsBuilder WithHttpStatusCode(int httpStatusCode)
        {
            _httpStatusCode = httpStatusCode;
            return this;
        }

        public RateLimitGlobalOptions Build()
        {
            return new RateLimitGlobalOptions(_clientIdHeader,
                _disableRateLimitHeaders, _quotaExceededMessage, _rateLimitCounterPrefix,
                _httpStatusCode);
        }
    }
}
