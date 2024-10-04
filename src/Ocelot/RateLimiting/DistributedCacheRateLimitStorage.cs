using Microsoft.Extensions.Caching.Distributed;
using Ocelot.Infrastructure;
using System.Text.Json;

namespace Ocelot.RateLimiting;

/// <summary>
/// Custom storage based on a distributed cache of a remote/local services.
/// </summary>
/// <remarks>
/// See the <see cref="IDistributedCache"/> interface docs for more details.
/// </remarks>
public class DistributedCacheRateLimitStorage : IRateLimitStorage
{
    private readonly IDistributedCache _memoryCache;

    public DistributedCacheRateLimitStorage(IDistributedCache memoryCache) => _memoryCache = memoryCache;

    public void Set(string id, RateLimitCounter counter, TimeSpan expirationTime)
        => _memoryCache.SetString(id, JsonSerializer.Serialize(counter, JsonSerializerOptionsFactory.Web), new DistributedCacheEntryOptions().SetAbsoluteExpiration(expirationTime));

    public bool Exists(string id) => !string.IsNullOrEmpty(_memoryCache.GetString(id));

    public RateLimitCounter? Get(string id)
    {
        var stored = _memoryCache.GetString(id);
        return !string.IsNullOrEmpty(stored)
            ? JsonSerializer.Deserialize<RateLimitCounter>(stored, JsonSerializerOptionsFactory.Web)
            : null;
    }

    public void Remove(string id) => _memoryCache.Remove(id);
}
