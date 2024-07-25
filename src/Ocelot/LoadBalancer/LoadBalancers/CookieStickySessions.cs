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

    private static readonly object _lock = new();
    private static readonly ConcurrentDictionary<string, StickySession> _stored = new(); // TODO Inject instead of static sharing

    public CookieStickySessions(ILoadBalancer loadBalancer, string cookieName, int keyExpiryInMs, IBus<StickySession> bus)
    {
        _bus = bus;
        _cookieName = cookieName;
        _keyExpiryInMs = keyExpiryInMs;
        _loadBalancer = loadBalancer;
        _bus.Subscribe(CheckExpiry);
    }

    private void CheckExpiry(StickySession sticky)
    {
        // TODO Get test coverage for this
        if (_stored.TryGetValue(sticky.Key, out var session))
        {
            lock (_lock)
            {
                if (session.Expiry < DateTime.UtcNow)
                {
                    _stored.TryRemove(session.Key, out _);
                    _loadBalancer.Release(session.HostAndPort);
                }
            }
        }
    }

    public Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
    {
        var route = httpContext.Items.DownstreamRoute();
        var serviceName = route.LoadBalancerKey;
        var cookie = httpContext.Request.Cookies[_cookieName];
        var key = $"{serviceName}:{cookie}"; // strong key name because of static store
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(key) && _stored.TryGetValue(key, out StickySession cached))
            {
                var updated = new StickySession(cached.HostAndPort, DateTime.UtcNow.AddMilliseconds(_keyExpiryInMs), key);
                Update(key, updated);
                return Task.FromResult<Response<ServiceHostAndPort>>(new OkResponse<ServiceHostAndPort>(updated.HostAndPort));
            }

            // There is no value in the store, so lease it now!
            var next = _loadBalancer.Lease(httpContext).GetAwaiter().GetResult(); // unfortunately the operation must be synchronous
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
        _stored[key] = value;
        _bus.Publish(value, _keyExpiryInMs);
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}
