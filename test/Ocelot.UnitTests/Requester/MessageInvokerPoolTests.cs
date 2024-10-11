using Microsoft.AspNetCore.Builder;
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
    private HttpMessageInvoker _firstInvoker;
    private HttpMessageInvoker _secondInvoker;
    private Mock<IDelegatingHandlerHandlerFactory> _handlerFactory;
    private readonly Mock<IOcelotLoggerFactory> _ocelotLoggerFactory;
    private readonly Mock<IOcelotLogger> _ocelotLogger;
    private HttpContext _context;
    private HttpResponseMessage _response;
    private IWebHost _host;

    public MessageInvokerPoolTests()
    {
        _ocelotLoggerFactory = new Mock<IOcelotLoggerFactory>();
        _ocelotLogger = new Mock<IOcelotLogger>();
        _ocelotLoggerFactory.Setup(x => x.CreateLogger<MessageInvokerPool>()).Returns(_ocelotLogger.Object);
    }

    [Fact]
    public void If_calling_the_same_downstream_route_twice_should_return_the_same_message_invoker()
    {
        this.Given(x => x.GivenADownstreamRoute("/super-test"))
            .And(x => x.AndAHandlerFactory())
            .And(x => x.GivenAMessageInvokerPool())
            .When(x => x.WhenGettingMessageInvokerTwice())
            .Then(x => x.ThenTheInvokersShouldBeTheSame())
            .BDDfy();
    }

    [Fact]
    public void If_calling_two_different_downstream_routes_should_return_different_message_invokers()
    {
        this.Given(x => x.GivenTwoDifferentDownstreamRoutes("/super-test", "/super-test"))
            .And(x => x.AndAHandlerFactory())
            .And(x => x.GivenAMessageInvokerPool())
            .When(x => x.WhenGettingMessageInvokerForBothRoutes())
            .Then(x => x.ThenTheInvokersShouldNotBeTheSame())
            .BDDfy();
    }

    [Fact]
    public void If_two_delegating_handlers_are_defined_then_these_should_be_call_in_order()
    {
        var fakeOne = new FakeDelegatingHandler();
        var fakeTwo = new FakeDelegatingHandler();

        var handlers = new List<Func<DelegatingHandler>>
        {
            () => fakeOne,
            () => fakeTwo,
        };

        this.Given(x => GivenTheFactoryReturns(handlers))
            .And(x => GivenADownstreamRoute("/super-test"))
            .And(x => GivenAMessageInvokerPool())
            .And(x => GivenARequest())
            .When(x => WhenICallTheClient("http://www.bbc.co.uk"))
            .Then(x => ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo))
            .And(x => ThenSomethingIsReturned())
            .BDDfy();
    }

    [Fact]
    public void Should_log_if_ignoring_ssl_errors()
    {
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

        this.Given(x => GivenTheFactoryReturns(new List<Func<DelegatingHandler>>()))
            .And(x => GivenAMessageInvokerPool())
            .And(x => GivenARequest(route))
            .When(x => WhenICallTheClient("http://www.bbc.co.uk"))
            .Then(x => ThenTheDangerousAcceptAnyServerCertificateValidatorWarningIsLogged())
            .BDDfy();
    }

    [Fact]
    public void Should_re_use_cookies_from_container()
    {
        var qosOptions = new QoSOptionsBuilder()
            .Build();

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(false, true, false, true, int.MaxValue, TimeSpan.FromSeconds(90)))
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())
            .WithQosOptions(new QoSOptionsBuilder().Build())
            .Build();

        this.Given(_ => GivenADownstreamService())
            .And(x => GivenTheFactoryReturns(new List<Func<DelegatingHandler>>()))
            .And(x => GivenAMessageInvokerPool())
            .And(x => GivenARequest(route))
            .And(_ => WhenICallTheClient("http://localhost:5003"))
            .And(_ => ThenTheCookieIsSet())
            .When(_ => WhenICallTheClient("http://localhost:5003"))
            .Then(_ => ThenTheResponseIsOk())
            .BDDfy();
    }

    [Theory]
    [Trait("Issue", "1833")]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    public void Create_TimeoutValueInQosOptions_MessageInvokerTimeout(int qosTimeout, int expectedSeconds)
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

        this.Given(x => GivenTheFactoryReturns(new List<Func<DelegatingHandler>>()))
            .And(x => GivenAMessageInvokerPool())
            .And(x => GivenARequest(route))
            .Then(x => WhenICallTheClientWillThrowAfterTimeout(TimeSpan.FromSeconds(expectedSeconds)))
            .BDDfy();
    }

    private void ThenTheDangerousAcceptAnyServerCertificateValidatorWarningIsLogged()
    {
        _ocelotLogger.Verify(x => x.LogWarning(
            It.Is<Func<string>>(y => y.Invoke() == $"You have ignored all SSL warnings by using DangerousAcceptAnyServerCertificateValidator for this DownstreamRoute, UpstreamPathTemplate: {_context.Items.DownstreamRoute().UpstreamPathTemplate}, DownstreamPathTemplate: {_context.Items.DownstreamRoute().DownstreamPathTemplate}")),
            Times.Once);
    }

    private void ThenTheCookieIsSet()
    {
        _response.Headers.TryGetValues("Set-Cookie", out var test).ShouldBeTrue();
    }

    private void GivenADownstreamService()
    {
        var count = 0;
        _host = new WebHostBuilder()
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

    private void ThenTheResponseIsOk()
    {
        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private void GivenARequest(DownstreamRoute downstream)
    {
        GivenARequest(downstream, "http://localhost:5003");
    }

    private void GivenARequest(DownstreamRoute downstream, string downstreamUrl)
    {
        GivenARequestWithAUrlAndMethod(downstream, downstreamUrl, HttpMethod.Get);
    }

    private void GivenADownstreamRoute(string path) => _downstreamRoute1 = DownstreamRouteFactory(path);

    private void GivenTwoDifferentDownstreamRoutes(string path1, string path2)
    {
        _downstreamRoute1 = DownstreamRouteFactory(path1);
        _downstreamRoute2 = DownstreamRouteFactory(path2);
    }

    private void AndAHandlerFactory() => _handlerFactory = GetHandlerFactory();

    private void GivenAMessageInvokerPool() =>
        _pool = new MessageInvokerPool(_handlerFactory.Object, _ocelotLoggerFactory.Object);

    private void WhenGettingMessageInvokerTwice()
    {
        _firstInvoker = _pool.Get(_downstreamRoute1);
        _secondInvoker = _pool.Get(_downstreamRoute1);
    }

    private void WhenGettingMessageInvokerForBothRoutes()
    {
        _firstInvoker = _pool.Get(_downstreamRoute1);
        _secondInvoker = _pool.Get(_downstreamRoute2);
    }

    private void ThenTheInvokersShouldBeTheSame() => Assert.Equal(_firstInvoker, _secondInvoker);

    private void ThenTheInvokersShouldNotBeTheSame() => Assert.NotEqual(_firstInvoker, _secondInvoker);

    private void GivenARequest(string url) => GivenARequestWithAUrlAndMethod(_downstreamRoute1, url, HttpMethod.Get);

    private void GivenARequest() =>
        GivenARequestWithAUrlAndMethod(_downstreamRoute1, "http://localhost:5003", HttpMethod.Get);

    private void GivenARequestWithAUrlAndMethod(DownstreamRoute downstream, string url, HttpMethod method)
    {
        _context = new DefaultHttpContext();
        _context.Items.UpsertDownstreamRoute(downstream);
        _context.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage
            { RequestUri = new Uri(url), Method = method }));
    }

    private void ThenSomethingIsReturned() => _response.ShouldNotBeNull();

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

    private Mock<IDelegatingHandlerHandlerFactory> GetHandlerFactory()
    {
        var handlerFactory = new Mock<IDelegatingHandlerHandlerFactory>();
        handlerFactory.Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(new()));
        return handlerFactory;
    }

    private DownstreamRoute DownstreamRouteFactory(string path)
    {
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamPathTemplate(path)
            .WithQosOptions(new QoSOptions(new FileQoSOptions()))
            .WithLoadBalancerKey(string.Empty)
            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue(string.Empty).Build())
            .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false, false, 10, TimeSpan.FromSeconds(120)))
            .WithUpstreamHttpMethod(new() { "Get" })
            .Build();

        return downstreamRoute;
    }
}
