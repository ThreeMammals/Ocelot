using System.Collections.Generic;

namespace Ocelot.Configuration.Builder
{
    public class RateLimitOptionsBuilder
    {
        private bool _enableRateLimiting;
        private string _clientIdHeader;
        private List<string> _clientWhitelist;
        private bool _disableRateLimitHeaders;
        private string _quotaExceededMessage;
        private string _rateLimitCounterPrefix;
        private RateLimitRule _rateLimitRule;
        private int _httpStatusCode;

        public RateLimitOptionsBuilder WithEnableRateLimiting(bool enableRateLimiting)
        {
            _enableRateLimiting = enableRateLimiting;
            return this;
        }

        public RateLimitOptionsBuilder WithClientIdHeader(string clientIdheader)
        {
            _clientIdHeader = clientIdheader;
            return this;
        }

        public RateLimitOptionsBuilder WithClientWhiteList(List<string> clientWhitelist)
        {
            _clientWhitelist = clientWhitelist;
            return this;
        }

        public RateLimitOptionsBuilder WithDisableRateLimitHeaders(bool disableRateLimitHeaders)
        {
            _disableRateLimitHeaders = disableRateLimitHeaders;
            return this;
        }

        public RateLimitOptionsBuilder WithQuotaExceededMessage(string quotaExceededMessage)
        {
            _quotaExceededMessage = quotaExceededMessage;
            return this;
        }

        public RateLimitOptionsBuilder WithRateLimitCounterPrefix(string rateLimitCounterPrefix)
        {
            _rateLimitCounterPrefix = rateLimitCounterPrefix;
            return this;
        }

        public RateLimitOptionsBuilder WithRateLimitRule(RateLimitRule rateLimitRule)
        {
            _rateLimitRule = rateLimitRule;
            return this;
        }

        public RateLimitOptionsBuilder WithHttpStatusCode(int httpStatusCode)
        {
            _httpStatusCode = httpStatusCode;
            return this;
        }

        public RateLimitOptions Build()
        {
            return new RateLimitOptions(_enableRateLimiting, _clientIdHeader, _clientWhitelist,
                _disableRateLimitHeaders, _quotaExceededMessage, _rateLimitCounterPrefix,
                _rateLimitRule, _httpStatusCode);
        }
    }
}
