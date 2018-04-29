using System.Threading.Tasks;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Collections;
using Ocelot.Middleware;
using Ocelot.UnitTests.Responder;
using System.Threading;
using System.Collections.Concurrent;

namespace Ocelot.UnitTests.LoadBalancer
{
    public class StickySessionsLoadBalancerTests
    {
        private StickySessions _stickySessions;
        private Mock<ILoadBalancer> _loadBalancer;
        private int _defaultExpiryInMs;

        public StickySessionsLoadBalancerTests()
        {
            _loadBalancer = new Mock<ILoadBalancer>();
            _defaultExpiryInMs = 100;
        }

        [Fact]
        public async Task should_return_host_and_port()
        {
            _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("", 80)));
            _stickySessions = new StickySessions(_loadBalancer.Object);
            var downstreamContext = new DownstreamContext(new DefaultHttpContext());

            var hostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", _defaultExpiryInMs);

            hostAndPort.Data.ShouldNotBeNull();
        }

        [Fact]
        public async Task should_return_same_host_and_port()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new StickySessions(_loadBalancer.Object);
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            var downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", _defaultExpiryInMs);
            var secondHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", _defaultExpiryInMs);

            firstHostAndPort.Data.DownstreamHost.ShouldBe(secondHostAndPort.Data.DownstreamHost);
            firstHostAndPort.Data.DownstreamPort.ShouldBe(secondHostAndPort.Data.DownstreamPort);
        }


        [Fact]
        public async Task should_return_different_host_and_port_if_load_balancer_does()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new StickySessions(_loadBalancer.Object);
            var contextOne = new DefaultHttpContext();
            var cookiesOne = new FakeCookies();
            cookiesOne.AddCookie("sessionid", "321");
            contextOne.Request.Cookies = cookiesOne;
            var contextTwo = new DefaultHttpContext();
            var cookiesTwo = new FakeCookies();
            cookiesTwo.AddCookie("sessionid", "123");
            contextTwo.Request.Cookies = cookiesTwo;

            var firstHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextOne), "sessionid", _defaultExpiryInMs);
            var secondHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextTwo), "sessionid", _defaultExpiryInMs);

            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);
            secondHostAndPort.Data.DownstreamHost.ShouldBe("two");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }

        [Fact]
        public async Task should_return_error()
        {
            _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(new ErrorResponse<ServiceHostAndPort>(new AnyError()));
            _stickySessions = new StickySessions(_loadBalancer.Object);
            var downstreamContext = new DownstreamContext(new DefaultHttpContext());

            var hostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", _defaultExpiryInMs);

            hostAndPort.IsError.ShouldBeTrue();
        }

        [Fact]
        public async Task should_expire_sticky_session()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new StickySessions(_loadBalancer.Object);
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            var downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", 100);
            var secondHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", 100);

            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);

            secondHostAndPort.Data.DownstreamHost.ShouldBe("one");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);

            await Task.Delay(150);

            var postExpireHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", 100);
            postExpireHostAndPort.Data.DownstreamHost.ShouldBe("two");
            postExpireHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }

        [Fact]
        public async Task should_refresh_sticky_session()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new StickySessions(_loadBalancer.Object);
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            var downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", 100);
            var secondHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", 100);

            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);

            secondHostAndPort.Data.DownstreamHost.ShouldBe("one");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);

            await Task.Delay(80);

            var postExpireHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid", 100);
            postExpireHostAndPort.Data.DownstreamHost.ShouldBe("one");
            postExpireHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }
    }

    public class StickySessions
    {
        private readonly ILoadBalancer _loadBalancer;
        private ConcurrentDictionary<string, StickySession> _stored;
        private Timer _timer;
        private bool _expiring;

        public StickySessions(ILoadBalancer loadBalancer)
        {
            _loadBalancer = loadBalancer;
            _stored = new ConcurrentDictionary<string, StickySession>();
            _timer = new Timer(x =>
            {
                if(_expiring)
                {
                    return;
                }

                _expiring = true;

                Expire();
                
                _expiring = false;

            }, null, 0, 50);
        }

        public async Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context, string cookieKey, int expiryInMs)
        {
            var value = context.HttpContext.Request.Cookies[cookieKey];

            if (!string.IsNullOrEmpty(value) && _stored.ContainsKey(value))
            {
                var cached = _stored[value];
                var updated = new StickySession(cached.HostAndPort, DateTime.UtcNow.AddMilliseconds(expiryInMs));
                _stored[value] = updated;
                return new OkResponse<ServiceHostAndPort>(updated.HostAndPort);
            }

            var next = await _loadBalancer.Lease();

            if (next.IsError)
            {
                return new ErrorResponse<ServiceHostAndPort>(next.Errors);
            }

            if (!string.IsNullOrEmpty(value) && !_stored.ContainsKey(value))
            {
                _stored[value] = new StickySession(next.Data, DateTime.UtcNow.AddMilliseconds(expiryInMs));
            }

            return new OkResponse<ServiceHostAndPort>(next.Data);
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
            _loadBalancer.Release(hostAndPort);
        }

        private void Expire()
        {
            var expired = _stored.Where(x => x.Value.Expiry < DateTime.UtcNow);

            foreach(var expire in expired)
            {
                _stored.Remove(expire.Key, out _);
            }
        }
    }

    public class StickySession
    {
        public StickySession(ServiceHostAndPort hostAndPort, DateTime expiry)
        {
            this.HostAndPort = hostAndPort;
            this.Expiry = expiry;

        }
        public ServiceHostAndPort HostAndPort { get; }
        public DateTime Expiry { get; }
    }

    class FakeCookies : IRequestCookieCollection
    {
        private Dictionary<string, string> _cookies = new Dictionary<string, string>();

        public string this[string key] => _cookies[key];

        public int Count => _cookies.Count;

        public ICollection<string> Keys => _cookies.Keys;

        public void AddCookie(string key, string value)
        {
            _cookies[key] = value;
        }

        public bool ContainsKey(string key)
        {
            return _cookies.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _cookies.GetEnumerator();
        }

        public bool TryGetValue(string key, out string value)
        {
            return _cookies.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _cookies.GetEnumerator();
        }
    }
}
