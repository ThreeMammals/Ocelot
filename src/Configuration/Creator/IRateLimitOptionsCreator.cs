using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IRateLimitOptionsCreator
{
    RateLimitOptions Create(FileGlobalConfiguration globalConfiguration);
    RateLimitOptions Create(IRouteRateLimiting route, FileGlobalConfiguration globalConfiguration);
}
