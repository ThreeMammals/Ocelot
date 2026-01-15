namespace Ocelot.RateLimiting;

/// <summary>
/// Defines a storage for keeping of rate limiting data.
/// </summary>
/// <remarks>Concrete classes should be based on solutions with excellent performance, such as in-memory solutions.</remarks>
public interface IRateLimitStorage
{
    bool Exists(string id);

    RateLimitCounter? Get(string id);

    void Remove(string id);

    void Set(string id, RateLimitCounter counter, TimeSpan expirationTime);
}
