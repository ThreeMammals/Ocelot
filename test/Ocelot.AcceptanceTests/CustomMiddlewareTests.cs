using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Ocelot.Middleware;
using System.Diagnostics;

namespace Ocelot.AcceptanceTests;

public class CustomMiddlewareTests : Steps
{
    private int _counter;

    public CustomMiddlewareTests()
    {
        _counter = 0;
    }

    [Fact]
    public void Should_call_pre_query_string_builder_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            AuthorizationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_authorization_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            AuthorizationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_authentication_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            AuthenticationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/41879/");
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_pre_error_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            PreErrorResponderMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_pre_authorization_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            PreAuthorizationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_pre_http_authentication_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            PreAuthenticationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_not_throw_when_pipeline_terminates_early()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            PreQueryStringBuilderMiddleware = (context, next) =>
                Task.Run(() =>
                {
                    _counter++;
                    return; // do not invoke the rest of the pipeline
                }),
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    /// <summary>
    /// This is just an example to show how you could hook into Ocelot pipeline with your own middleware.
    /// At the moment you must use Response.OnCompleted callback and cannot change the response :(
    /// I will see if this can be changed one day.
    /// </summary>
    [Fact]
    [Trait("Feat", "237")] // https://github.com/ThreeMammals/Ocelot/issues/237
    [Trait("PR", "241")] // https://github.com/ThreeMammals/Ocelot/pull/241
    [Trait("Release", "3.1.6")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.6
    public void Should_fix_issue_237()
    {
        Func<object, Task> callback = state =>
        {
            var context = (HttpContext)state;
            if (context.Response.StatusCode > 400)
            {
                Debug.WriteLine("COUNT CALLED");
                Console.WriteLine("COUNT CALLED");
            }
            return Task.CompletedTask;
        };

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/", "/west");
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, "/test"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithMiddlewareBeforePipeline<FakeMiddleware>(callback))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_call_after_http_authentication_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            AfterAuthenticationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    [Fact]
    public void Should_call_after_authorization_middleware()
    {
        var pipelineConfiguration = new OcelotPipelineConfiguration
        {
            AfterAuthorizationMiddleware = async (ctx, next) =>
            {
                _counter++;
                await next.Invoke();
            },
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOnPath(port, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(pipelineConfiguration))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => x.ThenTheCounterIs(1))
            .BDDfy();
    }

    private void GivenOcelotIsRunningWithMiddlewareBeforePipeline<T>(Func<object, Task> middleware)
    {
        var builder = TestHostBuilder.Create()
            .ConfigureAppConfiguration(WithBasicConfiguration)
            .ConfigureServices(WithAddOcelot)
            .Configure(async app => await app
                .UseMiddleware<T>(middleware)
                .UseOcelot());
        ocelotServer = new TestServer(builder);
        ocelotClient = ocelotServer.CreateClient();
    }

    private void ThenTheCounterIs(int expected)
    {
        _counter.ShouldBe(expected);
    }

    private void GivenThereIsAServiceRunningOnPath(int port, string basePath)
    {
        Task MapPath(HttpContext context)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else if (context.Request.Path.Value != basePath)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            return Task.CompletedTask;
        }
        handler.GivenThereIsAServiceRunningOn(port, MapPath);
    }

    public class FakeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Func<object, Task> _callback;

        public FakeMiddleware(RequestDelegate next, Func<object, Task> callback)
        {
            _next = next;
            _callback = callback;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            context.Response.OnCompleted(_callback, context);
        }
    }
}
