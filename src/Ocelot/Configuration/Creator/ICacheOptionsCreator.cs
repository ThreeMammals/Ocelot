using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// This interface is used to create cache options.
/// </summary>
public interface ICacheOptionsCreator
{
    /// <summary>
    /// Creates cache options based on the file cache options, upstream path template and upstream HTTP methods.</summary>
    /// <remarks>Upstream path template and upstream HTTP methods are used to get the region name.</remarks>
    /// <param name="options">The file cache options.</param>
    /// <param name="global">The global configuration.</param>
    /// <param name="upstreamPathTemplate">The upstream path template as string.</param>
    /// <param name="upstreamHttpMethods">The upstream http methods as a list of strings.</param>
    /// <returns>The generated cache options.</returns>
    CacheOptions Create(FileCacheOptions options, FileGlobalConfiguration global, string upstreamPathTemplate, IList<string> upstreamHttpMethods);
}
