namespace Ocelot.LoadBalancer;

public class InMemoryStickySessionStorage : IStickySessionStorage
{
    private readonly ConcurrentDictionary<string, StickySession> _storage;

    public InMemoryStickySessionStorage()
        => _storage = new();

    public bool TryGetSession(string key, out StickySession session)
        => _storage.TryGetValue(key, out session);

    public StickySession GetSession(string key)
        => _storage.TryGetValue(key, out var session) ? session : null;

    public void SetSession(string key, StickySession session)
        => _storage[key] = session;

    public bool TryRemove(string key, out StickySession session)
        => _storage.TryRemove(key, out session);

    public bool Contains(string key)
        => _storage.ContainsKey(key);
}
