﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester;
using Ocelot.Responses;
using System.Diagnostics;

namespace Ocelot.UnitTests.Requester;

[Trait("PR", "1824")]
public class MessageInvokerPoolTests : UnitTest
{
    private DownstreamRoute _downstreamRoute1;
    private DownstreamRoute _downstreamRoute2;
    private MessageInvokerPool _pool;
    private Mock<IDelegatingHandlerHandlerFactory> _handlerFactory;
    private readonly Mock<IOcelotLoggerFactory> _ocelotLoggerFactory;
    private readonly Mock<IOcelotLogger> _ocelotLogger;
    private readonly DefaultHttpContext _context;
    private HttpResponseMessage _response;
    private IWebHost _host;

    public MessageInvokerPoolTests()
    {
        _context = new();
        _ocelotLoggerFactory = new Mock<IOcelotLoggerFactory>();
        _ocelotLogger = new Mock<IOcelotLogger>();
        _ocelotLoggerFactory.Setup(x => x.CreateLogger<MessageInvokerPool>()).Returns(_ocelotLogger.Object);
    }

    [Fact]
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
        GivenARequestWithAUrlAndMethod(_downstreamRoute1, "http://localhost:5003", HttpMethod.Get);

        // Act
        await WhenICallTheClient("http://www.bbc.co.uk");

        // Assert
        ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo);
        _response.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_log_if_ignoring_ssl_errors()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false, true, int.MaxValue, TimeSpan.FromSeconds(90)))
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())
            .WithQosOptions(new QoSOptionsBuilder().Build())
            .WithDangerousAcceptAnyServerCertificateValidator(true)
            .Build();
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        GivenARequest(route);

        // Act
        await WhenICallTheClient("http://www.google.com/");

        // Assert: Then The DangerousAcceptAnyServerCertificateValidator Warning Is Logged
        _ocelotLogger.Verify(x => x.LogWarning(
            It.Is<Func<string>>(y => y.Invoke() == $"You have ignored all SSL warnings by using DangerousAcceptAnyServerCertificateValidator for this DownstreamRoute, UpstreamPathTemplate: {_context.Items.DownstreamRoute().UpstreamPathTemplate}, DownstreamPathTemplate: {_context.Items.DownstreamRoute().DownstreamPathTemplate}")),
            Times.Once);
    }

    [Fact]
    public async Task Should_re_use_cookies_from_container()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(false, true, false, true, int.MaxValue, TimeSpan.FromSeconds(90)))
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())
            .WithQosOptions(new QoSOptionsBuilder().Build())
            .Build();
        GivenADownstreamService();
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        GivenARequest(route);

        // Act, Assert
        await WhenICallTheClient("http://localhost:5003");
        _response.Headers.TryGetValues("Set-Cookie", out _).ShouldBeTrue();

        // Act, Assert
        await WhenICallTheClient("http://localhost:5003");
        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [Trait("Issue", "1833")]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    public async Task Create_TimeoutValueInQosOptions_MessageInvokerTimeout(int qosTimeout, int expectedSeconds)
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(qosTimeout * 1000)
            .Build();
        var handlerOptions = new HttpHandlerOptionsBuilder()
            .WithUseMaxConnectionPerServer(int.MaxValue)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(handlerOptions)
            .Build();
        GivenTheFactoryReturnsNothing();
        GivenTheFactoryReturns(new List<Func<DelegatingHandler>>());
        GivenAMessageInvokerPool();
        GivenARequest(route);

        // Act, Assert
        await WhenICallTheClientWillThrowAfterTimeout(TimeSpan.FromSeconds(expectedSeconds));
    }

    private void GivenADownstreamService()
    {
        var count = 0;
        _host = TestHostBuilder.Create()
            .UseUrls("http://localhost:5003")
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

        _host.Start();
    }

    private void GivenARequest(DownstreamRoute downstream)
    {
        GivenARequestWithAUrlAndMethod(downstream, "http://localhost:5003", HttpMethod.Get);
    }

    private void AndAHandlerFactory() => _handlerFactory = GetHandlerFactory();

    private void GivenAMessageInvokerPool() =>
        _pool = new MessageInvokerPool(_handlerFactory.Object, _ocelotLoggerFactory.Object);

    private void GivenARequestWithAUrlAndMethod(DownstreamRoute downstream, string url, HttpMethod method)
    {
        _context.Items.UpsertDownstreamRoute(downstream);
        _context.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage
            { RequestUri = new Uri(url), Method = method }));
    }

    private async Task WhenICallTheClient(string url)
    {
        var messageInvoker = _pool.Get(_context.Items.DownstreamRoute());
        _response = await messageInvoker
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, url), CancellationToken.None);
    }

    private async Task WhenICallTheClientWillThrowAfterTimeout(TimeSpan timeout)
    {
        var messageInvoker = _pool.Get(_context.Items.DownstreamRoute());
        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();
            _response = await messageInvoker
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;

            // Compare the elapsed time with the given timeout
            // You can use elapsed.CompareTo(timeout) or simply check if elapsed > timeout, based on your requirement
            Assert.IsType<TimeoutException>(e);
            Assert.True(elapsed >= timeout.Subtract(TimeSpan.FromMilliseconds(500)), $"Elapsed time {elapsed} is smaller than expected timeout {timeout} - 500 ms");
            Assert.True(elapsed < timeout.Add(TimeSpan.FromMilliseconds(500)), $"Elapsed time {elapsed} is bigger than expected timeout {timeout} + 500 ms");
        }
    }

    private static void ThenTheFakeAreHandledInOrder(FakeDelegatingHandler fakeOne, FakeDelegatingHandler fakeTwo) =>
        fakeOne.TimeCalled.ShouldBeGreaterThan(fakeTwo.TimeCalled);

    private void GivenTheFactoryReturnsNothing()
    {
        var handlers = new List<Func<DelegatingHandler>>();

        _handlerFactory = new Mock<IDelegatingHandlerHandlerFactory>();
        _handlerFactory
            .Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
    }

    private void GivenTheFactoryReturns(List<Func<DelegatingHandler>> handlers)
    {
        _handlerFactory = new Mock<IDelegatingHandlerHandlerFactory>();
        _handlerFactory
            .Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
    }

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
