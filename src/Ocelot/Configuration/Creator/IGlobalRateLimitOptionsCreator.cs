using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IGlobalRateLimitOptionsCreator
{
    IEnumerable<GlobalRateLimitOptions> Create(FileGlobalConfiguration globalConfiguration);
}
