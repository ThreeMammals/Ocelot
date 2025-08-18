using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IRateLimitOptionsCreator
{
    RateLimitOptions Create(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration);
    RateLimitOptions CreatePatternRules(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration);
}
