using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester;
using Ocelot.Responses;
using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;

namespace Ocelot.UnitTests.Requester;

public class MessageInvokerPoolTests : MessageInvokerPoolBase
{
    private DownstreamRoute _downstreamRoute1;
    private DownstreamRoute _downstreamRoute2;
    private readonly Mock<IOcelotLogger> _ocelotLogger;
    private IWebHost _host;

    public MessageInvokerPoolTests()
    {
        _ocelotLogger = new Mock<IOcelotLogger>();
        _ocelotLoggerFactory.Setup(x => x.CreateLogger<MessageInvokerPool>()).Returns(_ocelotLogger.Object);
    }

    [Fact]
    [Trait("PR", "1824")]
    public void If_calling_the_same_downstream_route_twice_should_return_the_same_message_invoker()
    {
        // Arrange
        _downstreamRoute1 = DownstreamRouteFactory("/super-test");
        AndAHandlerFactory();
        GivenAMessageInvokerPool();

        // Act
        var firstInvoker = _pool.Get(_downstreamRoute1);
        var secondInvoker = _pool.Get(_downstreamRoute1);

        // Assert
        Assert.Equal(firstInvoker, secondInvoker);
    }

    [Fact]
    [Trait("PR", "1824")]
    public void If_calling_two_different_downstream_routes_should_return_different_message_invokers()
    {
        // Arrange
        _downstreamRoute1 = DownstreamRouteFactory("/super-test");
        _downstreamRoute2 = DownstreamRouteFactory("/super-test");
        AndAHandlerFactory();
        GivenAMessageInvokerPool();

        // Act
        var firstInvoker = _pool.Get(_downstreamRoute1);
        var secondInvoker = _pool.Get(_downstreamRoute2);

        // Assert
        Assert.NotEqual(firstInvoker, secondInvoker);
    }

    [Fact]
    [Trait("PR", "1824")]
    public async Task If_two_delegating_handlers_are_defined_then_these_should_be_call_in_order()
    {
        // Arrange
        var fakeOne = new FakeDelegatingHandler();
        var fakeTwo = new FakeDelegatingHandler();
        var handlers = new List<Func<DelegatingHandler>>
        {
            () => fakeOne,
            () => fakeTwo,
        };
        GivenTheFactoryReturns(handlers);
        _downstreamRoute1 = DownstreamRouteFactory("/super-test");
        GivenAMessageInvokerPool();
        var port = PortFinder.GetRandomPort();
        GivenARequestWithAUrlAndMethod(_downstreamRoute1, $"http://localhost:{port}", HttpMethod.Get);

        // Act
        await WhenICallTheClient("http://www.bbc.co.uk");

        // Assert
        ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo);
        _response.ShouldNotBeNull();
    }

    /// <summary>120 seconds.</summary>
    private static TimeSpan DefaultPooledConnectionLifeTime => TimeSpan.FromSeconds(HttpHandlerOptionsCreator.DefaultPooledConnectionLifetimeSeconds);

    [Fact]
    [Trait("PR", "1824")]
    public async Task Should_log_if_ignoring_ssl_errors()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())
            .WithQosOptions(new QoSOptionsBuilder().Build())
            .WithDangerousAcceptAnyServerCertificateValidator(true)

            // The test should pass without timeout definition -> implicit default timeout
            //.WithTimeout(DownstreamRoute.DefaultTimeoutSeconds)
            .Build();
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        var port = PortFinder.GetRandomPort();
        GivenARequest(route, port);

        // Act
        await WhenICallTheClient("http://www.google.com/");

        // Assert: Then the DangerousAcceptAnyServerCertificateValidator warning is logged
        _ocelotLogger.Verify(
            x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == $"You have ignored all SSL warnings by using DangerousAcceptAnyServerCertificateValidator for this DownstreamRoute, UpstreamPathTemplate: {_context.Items.DownstreamRoute().UpstreamPathTemplate}, DownstreamPathTemplate: {_context.Items.DownstreamRoute().DownstreamPathTemplate}")),
            Times.Once);
    }

    // Actually it should be moved to acceptance testing because of usage of running downstream service host,
    // and the test requires a design review
    [Fact(Skip = nameof(SequentialTests) + ": It is unstable and should be tested in sequential mode")]
    [Trait("PR", "1824")]
    public async Task Should_reuse_cookies_from_container()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(false, true, false, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())
            .WithQosOptions(new QoSOptionsBuilder().Build())

            // The test should pass without timeout definition -> implicit default timeout
            //.WithTimeout(DownstreamRoute.DefaultTimeoutSeconds)
            .Build();

        //using ServiceHandler handler = new();
        var port = PortFinder.GetRandomPort();
        GivenADownstreamService(port); // sometimes it fails because of port binding

        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        GivenARequest(route, port);

        // Act, Assert
        var toUrl = Url(port);
        await WhenICallTheClient(toUrl);
        _response.Headers.TryGetValues("Set-Cookie", out _).ShouldBeTrue();

        // Act, Assert
        await WhenICallTheClient(toUrl);
        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #region PR 2073

    [Theory]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    [InlineData(1)]
    [InlineData(3)]
    public async Task SendAsync_NoQosAndHasRouteTimeout_ThrowTimeoutExceptionAfterRouteTimeout(int timeoutSeconds)
    {
        // Arrange
        var route = GivenRouteWithTimeouts(null, timeoutSeconds);
        GivenTheFactoryReturnsNothing();
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        var port = PortFinder.GetRandomPort();
        GivenARequest(route, port);

        // Act, Assert
        int marginMs = 50;
        var expected = TimeSpan.FromSeconds(timeoutSeconds);
        var watcher = await TestRetry.NoWaitAsync(
            () => WhenICallTheClientWillThrowAfterTimeout(expected, marginMs *= 2)); // call up to 3 times with margins 100, 200, 400
        AssertTimeoutPrecisely(watcher, expected);
    }

    [Theory]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public void CreateMessageInvoker_QosTimeoutAndRouteOne_CreatedTimeoutDelegatingHandlerWithoutQosTimeout(int qosTimeout, int routeTimeout)
    {
        // Arrange
        var route = GivenRouteWithTimeouts(qosTimeout, routeTimeout);
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        GivenARequest(route, PortFinder.GetRandomPort());

        // Act
        using var invoker = _pool.Get(_context.Items.DownstreamRoute());

        // Assert
        var actual = AssertTimeout(invoker, routeTimeout);
        Assert.NotEqual(qosTimeout, (int)actual.TotalSeconds);
    }

    [Theory]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    [InlineData(1, 2, 2, 0, "")] // QoS timeout < route timeout
    [InlineData(3, 4, 4, 0, "")] // QoS timeout < route timeout
    [InlineData(2, 1, 4, 1, "Route '/' has Quality of Service settings (QoSOptions) enabled, but either the route Timeout or the QoS TimeoutValue is misconfigured: specifically, the route Timeout (1000 ms) is shorter than the QoS TimeoutValue (2000 ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the QoS TimeoutValue and applied 4000 ms to the route Timeout. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!")] // QoS timeout > route timeout
    [InlineData(4, 3, 8, 1, "Route '/' has Quality of Service settings (QoSOptions) enabled, but either the route Timeout or the QoS TimeoutValue is misconfigured: specifically, the route Timeout (3000 ms) is shorter than the QoS TimeoutValue (4000 ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the QoS TimeoutValue and applied 8000 ms to the route Timeout. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!")] // QoS timeout > route timeout
    [InlineData(5, 5, 10, 1, "Route '/' has Quality of Service settings (QoSOptions) enabled, but either the route Timeout or the QoS TimeoutValue is misconfigured: specifically, the route Timeout (5000 ms) is equal to the QoS TimeoutValue (5000 ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the QoS TimeoutValue and applied 10000 ms to the route Timeout. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!")] // QoS timeout == route timeout
    [InlineData(DownstreamRoute.DefTimeout + 1, null, 2 * (DownstreamRoute.DefTimeout + 1), 1, "Route '/' has Quality of Service settings (QoSOptions) enabled, but either the DownstreamRoute.DefaultTimeoutSeconds or the QoS TimeoutValue is misconfigured: specifically, the DownstreamRoute.DefaultTimeoutSeconds (90000 ms) is shorter than the QoS TimeoutValue (91000 ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the QoS TimeoutValue and applied 182000 ms to the route Timeout instead of using DownstreamRoute.DefaultTimeoutSeconds. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!")] // DefaultTimeoutSeconds as route timeout
    public void EnsureRouteTimeoutIsGreaterThanQosOne_QosTimeoutVsRouteOne_ExpectedRouteTimeoutOrDoubledQosTimeout(int qosTimeout, int? routeTimeout, int expectedSeconds, int loggedCount, string expectedMessage)
    {
        // Arrange
        var route = GivenRouteWithTimeouts(qosTimeout, routeTimeout);
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        GivenARequest(route, PortFinder.GetRandomPort());
        Func<string> fMsg = null;
        _ocelotLogger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => fMsg = f);

        // Act
        using var invoker = _pool.Get(_context.Items.DownstreamRoute());

        // Assert
        Assert.NotEqual(expectedSeconds, qosTimeout);
        AssertTimeout(invoker, expectedSeconds);
        _ocelotLogger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Exactly(loggedCount));
        var message = fMsg?.Invoke() ?? string.Empty;
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    [InlineData(1, 2, "is shorter than")]
    [InlineData(2, 2, "is equal to")]
    [InlineData(3, 2, "is longer than")]
    public void EqualitySentence_ThreeCases(int left, int right, string expected)
    {
        // Arrange, Act
        var actual = MessageInvokerPool.EqualitySentence(left, right);

        // Assert
        Assert.Equal(expected, actual);
    }

    private static TimeSpan AssertTimeout(HttpMessageInvoker invoker, int expectedSeconds)
    {
        Assert.NotNull(invoker);
        Type me = invoker.GetType();
        var field = me.GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var handler = field.GetValue(invoker) as HttpMessageHandler;
        Assert.NotNull(handler);
        Assert.IsType<TimeoutDelegatingHandler>(handler);
        me = handler.GetType();
        field = me.GetField("_timeout", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var timeout = (TimeSpan)field.GetValue(handler);
        Assert.Equal(expectedSeconds, (int)timeout.TotalSeconds);
        return timeout;
    }
    #endregion

    private void GivenADownstreamService(int port)
    {
        var count = 0;
        _host = TestHostBuilder.Create()
            .UseUrls(Url(port))
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app =>
            {
                app.Run(context =>
                {
                    if (count == 0)
                    {
                        context.Response.Cookies.Append("test", "0");
                        context.Response.StatusCode = 200;
                        count++;
                        return Task.CompletedTask;
                    }

                    if (count == 1)
                    {
                        if (context.Request.Cookies.TryGetValue("test", out var cookieValue) ||
                            context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
                        {
                            context.Response.StatusCode = 200;
                            return Task.CompletedTask;
                        }

                        context.Response.StatusCode = 500;
                    }

                    return Task.CompletedTask;
                });
            })
            .Build();
        _host.Start(); // problematic starting in case of parallel running of unit tests because of failing of port binding
    }

    private void AndAHandlerFactory() => _handlerFactory = GetHandlerFactory();

    private async Task WhenICallTheClient(string url)
    {
        var messageInvoker = _pool.Get(_context.Items.DownstreamRoute());
        _response = await messageInvoker
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, url), CancellationToken.None);
    }

    private static void ThenTheFakeAreHandledInOrder(FakeDelegatingHandler fakeOne, FakeDelegatingHandler fakeTwo) =>
        fakeOne.TimeCalled.ShouldBeGreaterThan(fakeTwo.TimeCalled);

    private static Mock<IDelegatingHandlerHandlerFactory> GetHandlerFactory()
    {
        var handlerFactory = new Mock<IDelegatingHandlerHandlerFactory>();
        handlerFactory.Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(new()));
        return handlerFactory;
    }

    private static DownstreamRoute DownstreamRouteFactory(string path) => new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate(path)
            .WithQosOptions(new QoSOptions(new FileQoSOptions()))
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())
            .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false, false, 10, TimeSpan.FromSeconds(120)))
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();
}

[Collection(nameof(SequentialTests))]
public sealed class MessageInvokerPoolSequentialTests : MessageInvokerPoolBase
{
    [Fact]
    [Trait("Bug", "1833")]
    public async Task SendAsync_NoQosAndNoRouteTimeouts_ShouldTimeoutAfterDefaultSeconds()
    {
        // Arrange
        var route = GivenRouteWithTimeouts(null, null);
        GivenTheFactoryReturnsNothing();
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        GivenARequest(route, PortFinder.GetRandomPort());

        // Act, Assert
        DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.LowTimeout; // minimum possible
        try
        {
            int marginMs = 50;
            var expected = TimeSpan.FromSeconds(DownstreamRoute.LowTimeout);
            var watcher = await TestRetry.NoWaitAsync(
                () => WhenICallTheClientWillThrowAfterTimeout(expected, marginMs *= 2)); // call up to 3 times with margins 100, 200, 400
            AssertTimeoutPrecisely(watcher, expected);
        }
        finally
        {
            DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.DefTimeout;
        }
    }
}

public class MessageInvokerPoolBase : UnitTest
{
    protected Mock<IDelegatingHandlerHandlerFactory> _handlerFactory;
    protected HttpResponseMessage _response;
    protected MessageInvokerPool _pool;

    protected readonly DefaultHttpContext _context = new();
    protected readonly Mock<IOcelotLoggerFactory> _ocelotLoggerFactory = new();

    protected static DownstreamRoute GivenRouteWithTimeouts(int? qosTimeout, int? routeTimeout)
    {
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(qosTimeout.HasValue ? qosTimeout * 1000 : null) // !!!
            .Build();
        var handlerOptions = new HttpHandlerOptionsBuilder()
            .WithUseMaxConnectionPerServer(int.MaxValue)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(handlerOptions)
            .WithTimeout(routeTimeout) // !!!
            .WithUpstreamPathTemplate(new("/", 0, false, "/"))
            .Build();
        return route;
    }

    protected void GivenTheFactoryReturnsNothing()
    {
        var handlers = new List<Func<DelegatingHandler>>();
        _handlerFactory = new Mock<IDelegatingHandlerHandlerFactory>();
        _handlerFactory.Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
    }

    protected void GivenTheFactoryReturns(List<Func<DelegatingHandler>> handlers)
    {
        _handlerFactory = new Mock<IDelegatingHandlerHandlerFactory>();
        _handlerFactory.Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
    }

    protected void GivenAMessageInvokerPool() =>
        _pool = new MessageInvokerPool(_handlerFactory.Object, _ocelotLoggerFactory.Object);

    protected void GivenARequest(DownstreamRoute downstream, int port)
        => GivenARequestWithAUrlAndMethod(downstream, Url(port), HttpMethod.Get);
    protected void GivenARequestWithAUrlAndMethod(DownstreamRoute downstream, string url, HttpMethod method)
    {
        _context.Items.UpsertDownstreamRoute(downstream);
        _context.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage
        { RequestUri = new Uri(url), Method = method }));
    }

    protected async Task<Stopwatch> WhenICallTheClientWillThrowAfterTimeout(TimeSpan timeout, int marginMilliseconds)
    {
        var messageInvoker = _pool.Get(_context.Items.DownstreamRoute());
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        try
        {
            _response = await messageInvoker
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);
        }
        catch (Exception e)
        {
            Assert.IsType<TimeoutException>(e);
        }

        // Compare the elapsed time with the given timeout
        // You can use elapsed.CompareTo(timeout) or simply check if elapsed > timeout, based on your requirement
        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        var margin = TimeSpan.FromMilliseconds(marginMilliseconds);
        Assert.True(elapsed >= timeout.Subtract(margin), $"Elapsed time {elapsed} is smaller than expected timeout {timeout} - {marginMilliseconds}ms");
        Assert.True(elapsed < timeout.Add(margin), $"Elapsed time {elapsed} is bigger than expected timeout {timeout} + {marginMilliseconds}ms");
        return stopwatch;
    }

    protected static void AssertTimeoutPrecisely(Stopwatch watcher, TimeSpan expected, TimeSpan? precision = null)
    {
        precision ??= TimeSpan.FromMilliseconds(10);
        TimeSpan elapsed = watcher.Elapsed, margin = elapsed - expected;
        try
        {
            Assert.True(elapsed >= expected, $"Elapsed time {elapsed} is less than expected timeout {expected} with margin {margin}.");
        }
        catch (TrueException)
        {
            // The elapsed time is approximately 0.998xxx or 2.99xxx, with a 10ms margin of precision accepted.
            Assert.True(elapsed.Add(precision.Value) >= expected, $"Elapsed time {elapsed} is less than expected timeout {expected} with margin {margin} which module is >= {precision.Value.Milliseconds}ms.");
        }
    }

    protected static string Url(int port) => $"http://localhost:{port}";
}
