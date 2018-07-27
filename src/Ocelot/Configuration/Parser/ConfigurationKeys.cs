using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Configuration.Parser
{
    public static class ConfigurationKeys
    {
        public static class RateLimit
        {
            public const string CLIENT_WHITE_LIST = "rate_limit.client_white_list";
            public const string ENABLE_RATE_LIMITING = "rate_limit.enable_rate_limiting";
            public const string PERIOD = "rate_limit.period";
            public const string PERIOD_TIME_SPAN = "rate_limit.period_time_span";
            public const string LIMIT = "rate_limit.limit";
        }
    }
}
