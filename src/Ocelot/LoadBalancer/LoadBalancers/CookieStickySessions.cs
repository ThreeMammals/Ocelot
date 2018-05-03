namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Ocelot.Middleware;
    using Responses;
    using Values;

    public class CookieStickySessions : ILoadBalancer, IDisposable
    {
        private readonly int _keyExpiryInMs;
        private readonly string _key;
        private readonly ILoadBalancer _loadBalancer;
        private readonly ConcurrentDictionary<string, StickySession> _stored;
        private readonly Timer _timer;
        private bool _expiring;

        public CookieStickySessions(ILoadBalancer loadBalancer, string key, int keyExpiryInMs, int expiryPeriodInMs)
        {
            _key = key;
            _keyExpiryInMs = keyExpiryInMs;
            _loadBalancer = loadBalancer;
            _stored = new ConcurrentDictionary<string, StickySession>();
            _timer = new Timer(x =>
            {
                if (_expiring)
                {
                    return;
                }

                _expiring = true;

                Expire();

                _expiring = false;
            }, null, 0, expiryPeriodInMs);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public async Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context)
        {
            var value = context.HttpContext.Request.Cookies[_key];

            if (!string.IsNullOrEmpty(value) && _stored.ContainsKey(value))
            {
                var cached = _stored[value];

                var updated = new StickySession(cached.HostAndPort, DateTime.UtcNow.AddMilliseconds(_keyExpiryInMs));

                _stored[value] = updated;

                return new OkResponse<ServiceHostAndPort>(updated.HostAndPort);
            }

            var next = await _loadBalancer.Lease(context);

            if (next.IsError)
            {
                return new ErrorResponse<ServiceHostAndPort>(next.Errors);
            }

            if (!string.IsNullOrEmpty(value) && !_stored.ContainsKey(value))
            {
                _stored[value] = new StickySession(next.Data, DateTime.UtcNow.AddMilliseconds(_keyExpiryInMs));
            }

            return new OkResponse<ServiceHostAndPort>(next.Data);
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
        }

        private void Expire()
        {
            var expired = _stored.Where(x => x.Value.Expiry < DateTime.UtcNow);

            foreach (var expire in expired)
            {
                _stored.Remove(expire.Key, out _);
                _loadBalancer.Release(expire.Value.HostAndPort);
            }
        }
    }
}
