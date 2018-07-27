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

        public RateLimitOptions Create(FileReRoute fileReRoute, IInternalConfiguration configuration, bool enableRateLimiting)
        {
            RateLimitOptions rateLimitOption = null;

            if (enableRateLimiting)
            {
                rateLimitOption = new RateLimitOptionsBuilder()
                    .WithClientIdHeader(configuration.RateLimitOptions.ClientIdHeader)
                    .WithClientWhiteList(fileReRoute.RateLimitOptions.ClientWhitelist)
                    .WithDisableRateLimitHeaders(configuration.RateLimitOptions.DisableRateLimitHeaders)
                    .WithEnableRateLimiting(fileReRoute.RateLimitOptions.EnableRateLimiting)
                    .WithHttpStatusCode(configuration.RateLimitOptions.HttpStatusCode)
                    .WithQuotaExceededMessage(configuration.RateLimitOptions.QuotaExceededMessage)
                    .WithRateLimitCounterPrefix(configuration.RateLimitOptions.RateLimitCounterPrefix)
                    .WithRateLimitRule(new RateLimitRule(fileReRoute.RateLimitOptions.Period,
                        fileReRoute.RateLimitOptions.PeriodTimespan,
                        fileReRoute.RateLimitOptions.Limit))
                    .Build();
            }

            return rateLimitOption;
        }

        public RateLimitGlobalOptions Create(FileRateLimitOptions rateLimitOptions)
        {
            return new RateLimitGlobalOptionsBuilder()
                    .WithClientIdHeader(rateLimitOptions.ClientIdHeader)
                    .WithDisableRateLimitHeaders(rateLimitOptions.DisableRateLimitHeaders)
                    .WithHttpStatusCode(rateLimitOptions.HttpStatusCode)
                    .WithQuotaExceededMessage(rateLimitOptions.QuotaExceededMessage)
                    .WithRateLimitCounterPrefix(rateLimitOptions.RateLimitCounterPrefix)
                    .Build();
        }
    }
}
