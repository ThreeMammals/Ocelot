namespace Ocelot.Cache;

public interface IOcelotCache<T>
{
    /// <summary>
    /// Adds the specified <paramref name="value"/> to the cache.
    /// <para>Use this overload to overrule the configured expiration settings of the cache and to define a custom expiration <paramref name="ttl"/> for this <paramref name="value"/> only.</para>
    /// <para>The <c>Add</c> method will <b>not</b> be successful if the specified <paramref name="key"/> already exists within the cache.</para>
    /// </summary>
    /// <param name="key">The caching key.</param>
    /// <param name="value">The <c>CacheItem</c> to be added to the cache.</param>
    /// <param name="region">The region.</param>
    /// <param name="ttl">The timeout of absolute expiration.</param>
    /// <returns><see langword="true"/> if the key was not already added to the cache, <see langword="false"/> otherwise.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="key"/> or the <paramref name="value"/> is <see langword="null"/>.</exception>
    bool Add(string key, T value, string region, TimeSpan ttl);
    T AddOrUpdate(string key, T value, string region, TimeSpan ttl);

    T Get(string key, string region);

    void ClearRegion(string region);

    bool TryGetValue(string key, string region, out T value);
}
