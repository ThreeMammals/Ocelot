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

namespace Ocelot.UnitTests.LoadBalancer
{
    public class StickySessionsLoadBalancerTests
    {
        private StickySessions _stickySessions;
        private Mock<ILoadBalancer> _loadBalancer;

        public StickySessionsLoadBalancerTests()
        {
            _loadBalancer = new Mock<ILoadBalancer>();
        }

        [Fact]
        public async Task should_return_host_and_port()
        {
            _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("", 80)));

            _stickySessions = new StickySessions(_loadBalancer.Object);

            var downstreamContext = new DownstreamContext(new DefaultHttpContext());

            var hostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid");

            hostAndPort.Data.ShouldNotBeNull();
        }

        [Fact]
        public async Task should_return_same_host_and_port()
        {
            _loadBalancer
                .Setup(x => x.Lease())
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("", 80)));

            _stickySessions = new StickySessions(_loadBalancer.Object);

            var downstreamContext = new DownstreamContext(new DefaultHttpContext());

            var firstHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid");

            var secondHostAndPort = await _stickySessions.Lease(downstreamContext, "sessionid");

            firstHostAndPort.Data.DownstreamHost.ShouldBe(secondHostAndPort.Data.DownstreamHost);
            firstHostAndPort.Data.DownstreamPort.ShouldBe(secondHostAndPort.Data.DownstreamPort);
        }

        
        [Fact]
        public async Task should_return_same_host_and_port_for_different_requests()
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
            var firstHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextOne), "sessionid");
            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);

            var contextTwo = new DefaultHttpContext();
            var cookiesTwo = new FakeCookies();
            cookiesTwo.AddCookie("sessionid", "123");
            contextTwo.Request.Cookies = cookiesTwo;
            var secondHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextTwo), "sessionid");
            secondHostAndPort.Data.DownstreamHost.ShouldBe("two");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }
    }

    public class StickySessions
    {
        private readonly ILoadBalancer _loadBalancer;
        private Dictionary<string, ServiceHostAndPort> _stored;

        public StickySessions(ILoadBalancer loadBalancer)
        {
            _loadBalancer = loadBalancer;
            _stored = new Dictionary<string, ServiceHostAndPort>();
        }

        public async Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context, string cookieKey)
        {
            var value = context.HttpContext.Request.Cookies[cookieKey];

            if(!string.IsNullOrEmpty(value) && _stored.ContainsKey(value))
            {
                return new OkResponse<ServiceHostAndPort>(_stored[value]);
            }

            var next = await _loadBalancer.Lease();

            if(next.IsError)
            {
                return new ErrorResponse<ServiceHostAndPort>(next.Errors);
            }

            if(!string.IsNullOrEmpty(value) && _stored.ContainsKey(value))
            {
                _stored[value] = next.Data;
            }

            return new OkResponse<ServiceHostAndPort>(next.Data);
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
            _loadBalancer.Release(hostAndPort);
        }
    }

    public class FakeCookies : IRequestCookieCollection
    {
        private Dictionary<string,string> _cookies = new Dictionary<string,string>();

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
