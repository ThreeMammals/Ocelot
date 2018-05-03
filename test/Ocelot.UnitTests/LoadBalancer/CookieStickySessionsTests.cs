namespace Ocelot.UnitTests.LoadBalancer
{
    using System.Threading.Tasks;
    using Ocelot.LoadBalancer.LoadBalancers;
    using Ocelot.Responses;
    using Ocelot.Values;
    using Shouldly;
    using Xunit;
    using Moq;
    using Microsoft.AspNetCore.Http;
    using System.Collections.Generic;
    using System.Collections;
    using System.Threading;
    using Ocelot.Middleware;
    using Ocelot.UnitTests.Responder;
    using TestStack.BDDfy;

    public class CookieStickySessionsTests
    {
        private CookieStickySessions _stickySessions;
        private readonly Mock<ILoadBalancer> _loadBalancer;
        private readonly int _defaultExpiryInMs;
        private DownstreamContext _downstreamContext;
        private Response<ServiceHostAndPort> _result;
        private Response<ServiceHostAndPort> _firstHostAndPort;
        private Response<ServiceHostAndPort> _secondHostAndPort;

        public CookieStickySessionsTests()
        {
            _loadBalancer = new Mock<ILoadBalancer>();
            _defaultExpiryInMs = 100;
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs, 1);
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
        }

        [Fact]
        public void should_return_host_and_port()
        {
            this.Given(_ => GivenTheLoadBalancerReturns())
                .When(_ => WhenILease())
                .Then(_ => ThenTheHostAndPortIsNotNull())
                .BDDfy();
        }

        [Fact]
        public void should_return_same_host_and_port()
        {
            this.Given(_ => GivenTheLoadBalancerReturnsSequence())
                .And(_ => GivenTheDownstreamRequestHasSessionId("321"))
                .When(_ => WhenILeaseTwiceInARow())
                .Then(_ => ThenTheFirstAndSecondResponseAreTheSame())
                .BDDfy();
        }

        [Fact]
        public void should_return_different_host_and_port_if_load_balancer_does()
        {
            this.Given(_ => GivenTheLoadBalancerReturnsSequence())
                .When(_ => WhenIMakeTwoRequetsWithDifferentSessionValues())
                .Then(_ => ThenADifferentHostAndPortIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            this.Given(_ => GivenTheLoadBalancerReturnsError())
                .When(_ => WhenILease())
                .Then(_ => ThenAnErrorIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_expire_sticky_session()
        {
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs, 1);

            this.Given(_ => GivenTheLoadBalancerReturnsSequence())
                .When(_ => WhenTheStickySessionExpires())
                .Then(_ => ThenANewHostAndPortIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_refresh_sticky_session()
        {
            _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs, 50);

            this.Given(_ => GivenTheLoadBalancerReturnsSequence())
                .When(_ => WhenIMakeRequestsToKeepRefreshingTheSession())
                .Then(_ => ThenTheSessionIsRefreshed())
                .BDDfy();
        }

        [Fact]
        public void should_dispose()
        {
            _stickySessions.Dispose();
        }

        [Fact]
        public void should_release()
        {
            _stickySessions.Release(new ServiceHostAndPort("", 0));
        }

        private async Task ThenTheSessionIsRefreshed()
        {
            var postExpireHostAndPort = await _stickySessions.Lease(_downstreamContext);
            postExpireHostAndPort.Data.DownstreamHost.ShouldBe("one");
            postExpireHostAndPort.Data.DownstreamPort.ShouldBe(80);

            _loadBalancer
                .Verify(x => x.Lease(It.IsAny<DownstreamContext>()), Times.Once);
        }

        private async Task WhenIMakeRequestsToKeepRefreshingTheSession()
        {
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            _downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(_downstreamContext);
            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);

            Thread.Sleep(80);

            var secondHostAndPort = await _stickySessions.Lease(_downstreamContext);
            secondHostAndPort.Data.DownstreamHost.ShouldBe("one");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);

            Thread.Sleep(80);
        }

        private async Task ThenANewHostAndPortIsReturned()
        {
            var postExpireHostAndPort = await _stickySessions.Lease(_downstreamContext);
            postExpireHostAndPort.Data.DownstreamHost.ShouldBe("two");
            postExpireHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }

        private async Task WhenTheStickySessionExpires()
        {
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", "321");
            context.Request.Cookies = cookies;
            _downstreamContext = new DownstreamContext(context);

            var firstHostAndPort = await _stickySessions.Lease(_downstreamContext);
            var secondHostAndPort = await _stickySessions.Lease(_downstreamContext);

            firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            firstHostAndPort.Data.DownstreamPort.ShouldBe(80);

            secondHostAndPort.Data.DownstreamHost.ShouldBe("one");
            secondHostAndPort.Data.DownstreamPort.ShouldBe(80);

            Thread.Sleep(200);
        }

        private void ThenAnErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void GivenTheLoadBalancerReturnsError()
        {
            _loadBalancer
                .Setup(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new ErrorResponse<ServiceHostAndPort>(new AnyError()));
        }

        private void ThenADifferentHostAndPortIsReturned()
        {
            _firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
            _firstHostAndPort.Data.DownstreamPort.ShouldBe(80);
            _secondHostAndPort.Data.DownstreamHost.ShouldBe("two");
            _secondHostAndPort.Data.DownstreamPort.ShouldBe(80);
        }

        private async Task WhenIMakeTwoRequetsWithDifferentSessionValues()
        {
            var contextOne = new DefaultHttpContext();
            var cookiesOne = new FakeCookies();
            cookiesOne.AddCookie("sessionid", "321");
            contextOne.Request.Cookies = cookiesOne;
            var contextTwo = new DefaultHttpContext();
            var cookiesTwo = new FakeCookies();
            cookiesTwo.AddCookie("sessionid", "123");
            contextTwo.Request.Cookies = cookiesTwo;
            _firstHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextOne));
            _secondHostAndPort = await _stickySessions.Lease(new DownstreamContext(contextTwo));
        }

        private void GivenTheLoadBalancerReturnsSequence()
        {
            _loadBalancer
                .SetupSequence(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
        }

        private void ThenTheFirstAndSecondResponseAreTheSame()
        {
            _firstHostAndPort.Data.DownstreamHost.ShouldBe(_secondHostAndPort.Data.DownstreamHost);
            _firstHostAndPort.Data.DownstreamPort.ShouldBe(_secondHostAndPort.Data.DownstreamPort);
        }

        private async Task WhenILeaseTwiceInARow()
        {
            _firstHostAndPort = await _stickySessions.Lease(_downstreamContext);
            _secondHostAndPort = await _stickySessions.Lease(_downstreamContext);
        }

        private void GivenTheDownstreamRequestHasSessionId(string value)
        {
            var context = new DefaultHttpContext();
            var cookies = new FakeCookies();
            cookies.AddCookie("sessionid", value);
            context.Request.Cookies = cookies;
            _downstreamContext = new DownstreamContext(context);
        }

        private void GivenTheLoadBalancerReturns()
        {
            _loadBalancer
                .Setup(x => x.Lease(It.IsAny<DownstreamContext>()))
                .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("", 80)));
        }

        private async Task WhenILease()
        {
            _result = await _stickySessions.Lease(_downstreamContext);
        }

        private void ThenTheHostAndPortIsNotNull()
        {
            _result.Data.ShouldNotBeNull();
        }
    }
    
    class FakeCookies : IRequestCookieCollection
    {
        private readonly Dictionary<string, string> _cookies = new Dictionary<string, string>();

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
