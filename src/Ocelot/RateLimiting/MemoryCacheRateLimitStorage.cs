using Microsoft.Extensions.Caching.Memory;

namespace Ocelot.RateLimiting;

/// <summary>
/// Default storage based on the memory cache of the local web server instance.
/// </summary>
/// <remarks>
/// See the <see cref="IMemoryCache"/> interface docs for more details.
/// </remarks>
public class MemoryCacheRateLimitStorage : IRateLimitStorage
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheRateLimitStorage(IMemoryCache memoryCache) => _memoryCache = memoryCache;

    public void Set(string id, RateLimitCounter counter, TimeSpan expirationTime)
        => _memoryCache.Set(id, counter, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expirationTime));

    public bool Exists(string id) => _memoryCache.TryGetValue(id, out RateLimitCounter counter);

    public RateLimitCounter? Get(string id) => _memoryCache.TryGetValue(id, out RateLimitCounter counter) ? counter : null;

    public void Remove(string id) => _memoryCache.Remove(id);
}
