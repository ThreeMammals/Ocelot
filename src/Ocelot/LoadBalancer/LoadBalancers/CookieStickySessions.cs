using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers;

public class CookieStickySessions : ILoadBalancer
{
    private readonly int _keyExpiryInMs;
    private readonly string _cookieName;
    private readonly ILoadBalancer _loadBalancer;
    private readonly IBus<StickySession> _bus;

    private static readonly object Locker = new();
    private readonly IStickySessionStorage _storage;

    public string Type => nameof(CookieStickySessions);

    public CookieStickySessions(ILoadBalancer loadBalancer, string cookieName, int keyExpiryInMs, IBus<StickySession> bus, IStickySessionStorage storage)
    {
        _bus = bus;
        _cookieName = cookieName;
        _keyExpiryInMs = keyExpiryInMs;
        _loadBalancer = loadBalancer;
        _storage = storage;
        _bus.Subscribe(CheckExpiry);
    }

    private void CheckExpiry(StickySession sticky)
    {
        // TODO Get test coverage for this
        lock (Locker)
        {
            if (!_storage.TryGetSession(sticky.Key, out var session) || session.Expiry >= DateTime.UtcNow)
            {
                return;
            }

            _storage.TryRemove(session.Key, out _);
            _loadBalancer.Release(session.HostAndPort);
        }
    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var route = httpContext.Items.DownstreamRoute();
        var serviceName = route.LoadBalancerKey;
        var cookie = httpContext.Request.Cookies[_cookieName];
        var key = $"{serviceName}:{cookie}"; // strong key name because of static store
        lock (Locker)
        {
            if (!string.IsNullOrEmpty(key) && _storage.TryGetSession(key, out StickySession cached))
            {
                var updated = new StickySession(cached.HostAndPort, DateTime.UtcNow.AddMilliseconds(_keyExpiryInMs), key);
                Update(key, updated);
                return Task.FromResult<Response<ServiceHostAndPort>>(new OkResponse<ServiceHostAndPort>(updated.HostAndPort));
            }

            // There is no value in the store, so lease it now!
            var next = _loadBalancer.LeaseAsync(httpContext).GetAwaiter().GetResult(); // unfortunately the operation must be synchronous
            if (next.IsError)
            {
                return Task.FromResult<Response<ServiceHostAndPort>>(new ErrorResponse<ServiceHostAndPort>(next.Errors));
            }

            var ss = new StickySession(next.Data, DateTime.UtcNow.AddMilliseconds(_keyExpiryInMs), key);
            Update(key, ss);
            return Task.FromResult<Response<ServiceHostAndPort>>(new OkResponse<ServiceHostAndPort>(next.Data));
        }
    }

    protected void Update(string key, StickySession value)
    {
        lock (Locker)
        {
            _storage.SetSession(key, value);
            _bus.Publish(value, _keyExpiryInMs);
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}
