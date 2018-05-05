namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Ocelot.Infrastructure;
    using Ocelot.Middleware;
    using Responses;
    using Values;

    public class CookieStickySessions : ILoadBalancer, IDisposable
    {
        private readonly int _keyExpiryInMs;
        private readonly string _key;
        private readonly ILoadBalancer _loadBalancer;
        private readonly ConcurrentDictionary<string, StickySession> _stored;
        private IBus<StickySession> _bus;
        private readonly object _lock = new object();

        public CookieStickySessions(ILoadBalancer loadBalancer, string key, int keyExpiryInMs, IBus<StickySession> bus)
        {
            _bus = bus;
            _key = key;
            _keyExpiryInMs = keyExpiryInMs;
            _loadBalancer = loadBalancer;
            _stored = new ConcurrentDictionary<string, StickySession>();
            _bus.Subscribe(ss => {
                if(_stored.TryGetValue(ss.Key, out var stickySession))
                {
                    lock(_lock)
                    {
                        if(stickySession.Expiry < DateTime.Now)
                        {
                            _stored.Remove(stickySession.Key, out _);
                            _loadBalancer.Release(stickySession.HostAndPort);
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public async Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context)
        {
            var key = context.HttpContext.Request.Cookies[_key];
            
            if (!string.IsNullOrEmpty(key) && _stored.ContainsKey(key))
            {
                var cached = _stored[key];

                var updated = new StickySession(cached.HostAndPort, DateTime.UtcNow.AddMilliseconds(_keyExpiryInMs), key);

                _stored[key] = updated;

                await _bus.Publish(updated, _keyExpiryInMs);

                return new OkResponse<ServiceHostAndPort>(updated.HostAndPort);
            }

            var next = await _loadBalancer.Lease(context);

            if (next.IsError)
            {
                return new ErrorResponse<ServiceHostAndPort>(next.Errors);
            }

            if (!string.IsNullOrEmpty(key) && !_stored.ContainsKey(key))
            {
                var ss = new StickySession(next.Data, DateTime.UtcNow.AddMilliseconds(_keyExpiryInMs), key);
                _stored[key] = ss;
                await _bus.Publish(ss, _keyExpiryInMs);
            }

            return new OkResponse<ServiceHostAndPort>(next.Data);
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
        }
    }
}
