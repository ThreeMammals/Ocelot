using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.Infrastructure;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;
using Ocelot.Values;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.LoadBalancer;

public sealed class CookieStickySessionsTests : UnitTest
{
    private readonly CookieStickySessions _stickySessions;
    private readonly Mock<ILoadBalancer> _loadBalancer;
    private readonly int _defaultExpiryInMs;
    private readonly FakeBus<StickySession> _bus;
    private readonly DefaultHttpContext _httpContext;

    public CookieStickySessionsTests()
    {
        _httpContext = new DefaultHttpContext();
        _bus = new FakeBus<StickySession>();
        _loadBalancer = new Mock<ILoadBalancer>();
        _defaultExpiryInMs = 0;
        _stickySessions = new CookieStickySessions(_loadBalancer.Object, "sessionid", _defaultExpiryInMs, _bus);
    }

    private void Arrange([CallerMemberName] string serviceName = null)
    {
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerKey(serviceName)
            .Build();
        _httpContext.Items.UpsertDownstreamRoute(route);
    }

    [Fact]
    public async Task Should_expire_sticky_session()
    {
        Arrange();
        GivenTheLoadBalancerReturns();
        GivenTheDownstreamRequestHasSessionId("321");
        GivenIHackAMessageInWithAPastExpiry();

        // Act
        var result = await _stickySessions.LeaseAsync(_httpContext);
        _bus.Process();

        // Assert
        _loadBalancer.Verify(x => x.Release(It.IsAny<ServiceHostAndPort>()), Times.Once);
    }

    [Fact]
    public async Task Should_return_host_and_port()
    {
        Arrange();
        GivenTheLoadBalancerReturns();
        GivenTheDownstreamRequestHasSessionId("321");

        // Act
        var result = await _stickySessions.LeaseAsync(_httpContext);

        // Assert
        result.Data.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_return_same_host_and_port()
    {
        Arrange();
        GivenTheLoadBalancerReturnsSequence();
        GivenTheDownstreamRequestHasSessionId("321");

        // Act
        var firstHostAndPort = await _stickySessions.LeaseAsync(_httpContext);
        var secondHostAndPort = await _stickySessions.LeaseAsync(_httpContext);

        // Assert
        firstHostAndPort.Data.DownstreamHost.ShouldBe(secondHostAndPort.Data.DownstreamHost);
        firstHostAndPort.Data.DownstreamPort.ShouldBe(secondHostAndPort.Data.DownstreamPort);
        _bus.Messages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_return_different_host_and_port_if_load_balancer_does()
    {
        Arrange();
        GivenTheLoadBalancerReturnsSequence();

        // When I Make Two Requets With Different Session Values
        var contextOne = new DefaultHttpContext();
        var cookiesOne = new FakeCookies();
        cookiesOne.AddCookie("sessionid", "321");
        contextOne.Request.Cookies = cookiesOne;
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerKey(nameof(Should_return_different_host_and_port_if_load_balancer_does))
            .Build();
        contextOne.Items.UpsertDownstreamRoute(route);

        var contextTwo = new DefaultHttpContext();
        var cookiesTwo = new FakeCookies();
        cookiesTwo.AddCookie("sessionid", "123");
        contextTwo.Request.Cookies = cookiesTwo;
        contextTwo.Items.UpsertDownstreamRoute(route);

        // Act
        var firstHostAndPort = await _stickySessions.LeaseAsync(contextOne);
        var secondHostAndPort = await _stickySessions.LeaseAsync(contextTwo);

        // Assert
        firstHostAndPort.Data.DownstreamHost.ShouldBe("one");
        firstHostAndPort.Data.DownstreamPort.ShouldBe(80);
        secondHostAndPort.Data.DownstreamHost.ShouldBe("two");
        secondHostAndPort.Data.DownstreamPort.ShouldBe(80);
    }

    [Fact]
    public async Task Should_return_error()
    {
        Arrange();
        _loadBalancer.Setup(x => x.LeaseAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new ErrorResponse<ServiceHostAndPort>(new AnyError()));

        // Act
        var result = await _stickySessions.LeaseAsync(_httpContext);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void Should_release()
    {
        // Arrange, Act, Assert
        _stickySessions.Release(new ServiceHostAndPort(string.Empty, 0));
    }

    private void GivenIHackAMessageInWithAPastExpiry()
    {
        var hostAndPort = new ServiceHostAndPort("999", 999);
        _bus.Publish(new StickySession(hostAndPort, DateTime.UtcNow.AddDays(-1), "321"), 0);
    }

    private void GivenTheLoadBalancerReturnsSequence()
    {
        _loadBalancer
            .SetupSequence(x => x.LeaseAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("one", 80)))
            .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort("two", 80)));
    }

    private void GivenTheDownstreamRequestHasSessionId(string value)
    {
        var cookies = new FakeCookies();
        cookies.AddCookie("sessionid", value);
        _httpContext.Request.Cookies = cookies;
    }

    private void GivenTheLoadBalancerReturns()
    {
        _loadBalancer
            .Setup(x => x.LeaseAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new OkResponse<ServiceHostAndPort>(new ServiceHostAndPort(string.Empty, 80)));
    }
}

internal class FakeCookies : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies = new();
    public string this[string key] => _cookies[key];
    public int Count => _cookies.Count;
    public ICollection<string> Keys => _cookies.Keys;
    public void AddCookie(string key, string value) => _cookies[key] = value;
    public bool ContainsKey(string key) => _cookies.ContainsKey(key);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();
    public bool TryGetValue(string key, out string value) => _cookies.TryGetValue(key, out value);
    IEnumerator IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
}

internal class FakeBus<T> : IBus<T>
{
    public FakeBus()
    {
        Messages = new List<T>();
        Subscriptions = new List<Action<T>>();
    }

    public List<T> Messages { get; }
    public List<Action<T>> Subscriptions { get; }

    public void Subscribe(Action<T> action) => Subscriptions.Add(action);
    public void Publish(T message, int delay) => Messages.Add(message);

    public void Process()
    {
        foreach (var message in Messages)
        {
            foreach (var subscription in Subscriptions)
            {
                subscription(message);
            }
        }
    }
}
