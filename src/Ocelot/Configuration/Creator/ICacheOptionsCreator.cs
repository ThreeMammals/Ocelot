using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// This interface is used to create cache options.
/// </summary>
public interface ICacheOptionsCreator
{
    CacheOptions Create(FileCacheOptions options);
    CacheOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration, string loadBalancingKey);
    CacheOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration, string loadBalancingKey);
}
