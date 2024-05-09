using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

/// <summary>
/// This interface is used to create cache options.
/// </summary>
public interface ICacheOptionsCreator
{
    /// <summary>
    /// Creates cache options based on the file cache options, upstream path template and upstream HTTP methods.
    /// Upstream path template and upstream HTTP methods are used to get the region name.
    /// </summary>
    /// <param name="fileCacheOptions">The file cache options.</param>
    /// <param name="upstreamPathTemplate">The upstream path template as string.</param>
    /// <param name="upstreamHttpMethods">The upstream http methods as a list of strings.</param>
    /// <returns>The generated cache options.</returns>
    CacheOptions Create(FileCacheOptions fileCacheOptions, string upstreamPathTemplate, IList<string> upstreamHttpMethods);
}
