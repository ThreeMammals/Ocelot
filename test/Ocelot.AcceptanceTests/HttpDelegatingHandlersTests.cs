using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests;

public sealed class HttpDelegatingHandlersTests : Steps
{
    private string _downstreamPath;

    public HttpDelegatingHandlersTests()
    {
    }

    [Fact]
    public void Should_call_route_ordered_specific_handlers()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.DelegatingHandlers = new()
        {
            "FakeHandlerTwo",
            "FakeHandler",
        };
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithSpecificHandlersRegisteredInDi<FakeHandler, FakeHandlerTwo>())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheOrderedHandlersAreCalledCorrectly())
            .BDDfy();
    }

    [Fact]
    public void Should_call_global_di_handlers()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithGlobalHandlersRegisteredInDi<FakeHandler, FakeHandlerTwo>())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheHandlersAreCalledCorrectly())
            .BDDfy();
    }

    [Fact]
    public void Should_call_global_di_handlers_multiple_times()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithDelegatingHandler<FakeHandlerAgain>(true))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    public void Should_call_global_di_handlers_with_dependency()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);
        var dependency = new FakeDependency();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithGlobalHandlersRegisteredInDi<FakeHandlerWithDependency>(dependency))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheDependencyIsCalled(dependency))
            .BDDfy();
    }

    private static FileRoute GivenRoute(int port) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        DownstreamHostAndPorts = new()
        {
            new("localhost", port),
        },
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = new() { HttpMethods.Get },
    };

    private void GivenOcelotIsRunningWithSpecificHandlersRegisteredInDi<THandler1, THandler2>()
        where THandler1 : DelegatingHandler
        where THandler2 : DelegatingHandler
    {
        GivenOcelotIsRunning(s => s
            .AddOcelot()
            .AddDelegatingHandler<THandler1>()
            .AddDelegatingHandler<THandler2>());
    }

    private void GivenOcelotIsRunningWithGlobalHandlersRegisteredInDi<THandler1, THandler2>()
        where THandler1 : DelegatingHandler
        where THandler2 : DelegatingHandler
    {
        GivenOcelotIsRunning(s => s
            .AddOcelot()
            .AddDelegatingHandler<THandler1>(true)
            .AddDelegatingHandler<THandler2>(true));
    }

    private void GivenOcelotIsRunningWithGlobalHandlersRegisteredInDi<THandler>(FakeDependency dependency)
        where THandler : DelegatingHandler
    {
        GivenOcelotIsRunning(s => s
            .AddSingleton(dependency)
            .AddOcelot()
            .AddDelegatingHandler<THandler>(true));
    }

    private static void ThenTheDependencyIsCalled(FakeDependency dependency)
        => dependency.Called.ShouldBeTrue();
    private static void ThenTheHandlersAreCalledCorrectly()
        => FakeHandler.TimeCalled.ShouldBeLessThan(FakeHandlerTwo.TimeCalled);
    private static void ThenTheOrderedHandlersAreCalledCorrectly()
        => FakeHandlerTwo.TimeCalled.ShouldBeLessThan(FakeHandler.TimeCalled);

    public class FakeDependency
    {
        public bool Called;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class FakeHandlerWithDependency : DelegatingHandler
    {
        private readonly FakeDependency _dependency;

        public FakeHandlerWithDependency(FakeDependency dependency)
        {
            _dependency = dependency;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _dependency.Called = true;
            return base.SendAsync(request, cancellationToken);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class FakeHandler : DelegatingHandler
    {
        public static DateTime TimeCalled { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            TimeCalled = DateTime.Now;
            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellation);
            return await base.SendAsync(request, cancellation);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class FakeHandlerTwo : DelegatingHandler
    {
        public static DateTime TimeCalled { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            TimeCalled = DateTime.Now;
            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellation);
            return await base.SendAsync(request, cancellation);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class FakeHandlerAgain : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            Console.WriteLine(request.RequestUri);

            //do stuff and optionally call the base handler..
            return await base.SendAsync(request, cancellation);
        }
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool match = _downstreamPath == basePath;
            context.Response.StatusCode = match ? (int)statusCode : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(match ? responseBody : nameof(HttpStatusCode.NotFound));
        });
    }
}
