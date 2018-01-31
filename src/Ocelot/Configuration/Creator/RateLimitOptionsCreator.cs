using System;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class RateLimitOptionsCreator : IRateLimitOptionsCreator
    {
        public RateLimitOptions Create(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration, bool enableRateLimiting)
        {
            RateLimitOptions rateLimitOption = null;

            if (enableRateLimiting)
            {
                rateLimitOption = new RateLimitOptionsBuilder()
                    .WithClientIdHeader(globalConfiguration.RateLimitOptions.ClientIdHeader)
                    .WithClientWhiteList(fileReRoute.RateLimitOptions.ClientWhitelist)
                    .WithDisableRateLimitHeaders(globalConfiguration.RateLimitOptions.DisableRateLimitHeaders)
                    .WithEnableRateLimiting(fileReRoute.RateLimitOptions.EnableRateLimiting)
                    .WithHttpStatusCode(globalConfiguration.RateLimitOptions.HttpStatusCode)
                    .WithQuotaExceededMessage(globalConfiguration.RateLimitOptions.QuotaExceededMessage)
                    .WithRateLimitCounterPrefix(globalConfiguration.RateLimitOptions.RateLimitCounterPrefix)
                    .WithRateLimitRule(new RateLimitRule(fileReRoute.RateLimitOptions.Period,
                        fileReRoute.RateLimitOptions.PeriodTimespan,
                        fileReRoute.RateLimitOptions.Limit))
                    .Build();
            }

            return rateLimitOption;
        }
    }
}
