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
    public class CookieStickySessionsTests
    {
        private CookieStickySessions _stickySessions;
        private Mock<ILoadBalancer> _loadBalancer;
        private int _defaultExpiryInMs;

        public CookieStickySessionsTests()
        {
            _loadBalancer = new Mock<ILoadBalancer>();
            _defaultExpiryInMs = 100;
        }

        [Fact]
        public async Task should_return_host_and_port()
        {
            _loadBalancer
                .Setup(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("", 80)));
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs);
            var downstreamContext = new DownstreamContext(new DefaultHttpContext());

            var hostAndPort = await _stickySessions.Lease(downstreamContext);

            hostAndPort.Data.ShouldNotBeNull();
        }

        [Fact]
        public async Task should_return_same_host_and_port()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs);
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            var downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(downstreamContext);
            var secondHostAndPort = await _stickySessions.Lease(downstreamContext);

            firstHostAndPort.Data.DownstreamHost.ShouldBe(secondHostAndPort.Data.DownstreamHost);
            firstHostAndPort.Data.DownstreamPort.ShouldBe(secondHostAndPort.Data.DownstreamPort);
        }


        [Fact]
        public async Task should_return_different_host_and_port_if_load_balancer_does()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs);
            var contextOne = new DefaultHttpContext();
            var cookiesOne = new FakeCookies();
            cookiesOne.AddCookie("sessionid", "321");
            contextOne.Request.Cookies = cookiesOne;
            var contextTwo = new DefaultHttpContext();
            var cookiesTwo = new FakeCookies();
            cookiesTwo.AddCookie("sessionid", "123");
            contextTwo.Request.Cookies = cookiesTwo;

            var firstHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextOne));
            var secondHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextTwo));

            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);
            secondHostAndPort.Data.DownstreamHost.ShouldBe("two");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }

        [Fact]
        public async Task should_return_error()
        {
            _loadBalancer
                .Setup(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new ErrorResponse<ServiceHostAndPort>(new AnyError()));
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs);
            var downstreamContext = new DownstreamContext(new DefaultHttpContext());

            var hostAndPort = await _stickySessions.Lease(downstreamContext);

            hostAndPort.IsError.ShouldBeTrue();
        }

        [Fact]
        public async Task should_expire_sticky_session()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", 100);
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            var downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(downstreamContext);
            var secondHostAndPort = await _stickySessions.Lease(downstreamContext);

            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);

            secondHostAndPort.Data.DownstreamHost.ShouldBe("one");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);

            await Task.Delay(150);

            var postExpireHostAndPort = await _stickySessions.Lease(downstreamContext);
            postExpireHostAndPort.Data.DownstreamHost.ShouldBe("two");
            postExpireHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }

        [Fact]
        public async Task should_refresh_sticky_session()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", 100);
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            var downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(downstreamContext);
            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);

            await Task.Delay(60);

            var secondHostAndPort = await _stickySessions.Lease(downstreamContext);
            secondHostAndPort.Data.DownstreamHost.ShouldBe("one");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);

            await Task.Delay(60);

            var postExpireHostAndPort = await _stickySessions.Lease(downstreamContext);
            postExpireHostAndPort.Data.DownstreamHost.ShouldBe("one");
            postExpireHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }
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
