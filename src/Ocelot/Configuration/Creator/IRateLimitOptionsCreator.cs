using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IRateLimitOptionsCreator
    {
        RateLimitOptions Create(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration, bool enableRateLimiting);
    }
}