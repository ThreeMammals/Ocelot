namespace Ocelot.LoadBalancer;

public interface IStickySessionStorage
{
    bool Contains(string key);
    StickySession GetSession(string key);
    void SetSession(string key, StickySession session);
    bool TryGetSession(string key, out StickySession session);
    bool TryRemove(string key, out StickySession session);
}
