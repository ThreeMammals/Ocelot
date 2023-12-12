using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Requester;

public class MessageInvokerPoolTests
{
    private DownstreamRoute _downstreamRoute1;
    private DownstreamRoute _downstreamRoute2;
    private MessageInvokerPool _pool;
    private HttpMessageInvoker _firstInvoker;
    private HttpMessageInvoker _secondInvoker;
    private Mock<IDelegatingHandlerHandlerFactory> _handlerFactory;
    private readonly Mock<IOcelotLogger> _ocelotLogger;
    private HttpContext _context;
    private HttpResponseMessage _response;
    private IWebHost _host;

    public MessageInvokerPoolTests()
    {
        _ocelotLogger = new Mock<IOcelotLogger>();
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
    public void should_get_from_cache_with_different_query_string()
    {

        this.Given(x => x.GivenADownstreamRoute("/super-test"))
            .And(x => GivenTheFactoryReturns())
            .And(x => GivenARequest("http://wwww.someawesomewebsite.com/woot?badman=1"))

        this.Given(x => GivenARealCache())
            .And(x => GivenTheFactoryReturns())
            .And(x => GivenARequest(route, "http://wwww.someawesomewebsite.com/woot?badman=1"))
            .And(x => WhenIBuildTheFirstTime())
            .And(x => WhenISave())
            .And(x => WhenIBuildAgain())
            .And(x => GivenARequest(route, "http://wwww.someawesomewebsite.com/woot?badman=2"))
            .And(x => WhenISave())
            .When(x => WhenIBuildAgain())
            .Then(x => ThenTheHttpClientIsFromTheCache())
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
            .When(x => WhenICallTheClient())
            .Then(x => ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo))
            .And(x => ThenSomethingIsReturned())
            .BDDfy();
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
                app.Run(async context =>
                {
                    if (count == 0)
                    {
                        context.Response.Cookies.Append("test", "0");
                        context.Response.StatusCode = 200;
                        count++;
                        return;
                    }

                    if (count == 1)
                    {
                        if (context.Request.Cookies.TryGetValue("test", out var cookieValue) ||
                            context.Request.Headers.TryGetValue("Set-Cookie", out var headerValue))
                        {
                            context.Response.StatusCode = 200;
                            return;
                        }

                        context.Response.StatusCode = 500;
                    }

                    return;
                });
            })
            .Build();

        _host.Start();
    }

    private void GivenADownstreamRoute(string path) => _downstreamRoute1 = DownstreamRouteFactory(path);

    private void GivenTwoDifferentDownstreamRoutes(string path1, string path2)
    {
        _downstreamRoute1 = DownstreamRouteFactory(path1);
        _downstreamRoute2 = DownstreamRouteFactory(path2);
    }

    private void AndAHandlerFactory() => _handlerFactory = GetHandlerFactory();

    private void GivenAMessageInvokerPool() =>
        _pool = new MessageInvokerPool(_handlerFactory.Object, _ocelotLogger.Object);

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

    private void WhenICallTheClient()
    {
        var messageInvoker = _pool.Get(_context.Items.DownstreamRoute());
        _response = messageInvoker
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None).GetAwaiter()
            .GetResult();
    }

    private static void ThenTheFakeAreHandledInOrder(FakeDelegatingHandler fakeOne, FakeDelegatingHandler fakeTwo) =>
        fakeOne.TimeCalled.ShouldBeGreaterThan(fakeTwo.TimeCalled);

    private void GivenTheFactoryReturns()
    {
        var handlers = new List<Func<DelegatingHandler>> { () => new FakeDelegatingHandler() };

        _handlerFactory
            .Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
    }

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
            .Returns(new OkResponse<List<Func<DelegatingHandler>>>(new List<Func<DelegatingHandler>>()));
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
            .WithUpstreamHttpMethod(new List<string> { "Get" })
            .Build();

        return downstreamRoute;
    }
}
