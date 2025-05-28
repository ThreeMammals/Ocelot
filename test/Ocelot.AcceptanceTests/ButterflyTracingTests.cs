using Butterfly.Client.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Tracing.Butterfly;
using Xunit.Abstractions;

namespace Ocelot.AcceptanceTests;

public sealed class ButterflyTracingTests : Steps
{
    private int _butterflyCalled;
    private readonly ITestOutputHelper _output;

    public ButterflyTracingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Should_forward_tracing_information_from_ocelot_and_downstream_services()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/values",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port1,
                            },
                        },
                        UpstreamPathTemplate = "/api001/values",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            UseTracing = true,
                        },
                    },
                    new()
                    {
                        DownstreamPathTemplate = "/api/values",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port2,
                            },
                        },
                        UpstreamPathTemplate = "/api002/values",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            UseTracing = true,
                        },
                    },
                },
        };
        var butterflyPort = PortFinder.GetRandomPort();
        this.Given(x => GivenFakeButterfly(butterflyPort))
            .And(x => GivenServiceIsRunning(port1, "/api/values", HttpStatusCode.OK, "Hello from Laura", butterflyPort, "Service One"))
            .And(x => GivenServiceIsRunning(port2, "/api/values", HttpStatusCode.OK, "Hello from Tom", butterflyPort, "Service One"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingButterfly(butterflyPort))
            .When(x => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(x => WhenIGetUrlOnTheApiGateway("/api002/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Tom"))
            .BDDfy();

        var commandOnAllStateMachines = Wait.For(10_000).Until(() => _butterflyCalled >= 4);
        _output.WriteLine($"_butterflyCalled is {_butterflyCalled}");
        commandOnAllStateMachines.ShouldBeTrue();
    }

    [Fact]
    public void Should_return_tracing_header()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/values",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/api001/values",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            UseTracing = true,
                        },
                        DownstreamHeaderTransform = new Dictionary<string, string>
                        {
                            {"Trace-Id", "{TraceId}"},
                            {"Tom", "Laura"},
                        },
                    },
                },
        };
        var butterflyPort = PortFinder.GetRandomPort();
        this.Given(x => GivenFakeButterfly(butterflyPort))
            .And(x => GivenServiceIsRunning(port, "/api/values", HttpStatusCode.OK, "Hello from Laura", butterflyPort, "Service One"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingButterfly(butterflyPort))
            .When(x => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheResponseHeaderExists("Trace-Id"))
            .And(x => ThenTheResponseHeaderIs("Tom", "Laura"))
            .BDDfy();
    }

    private void GivenOcelotIsRunningUsingButterfly(int butterflyPort)
    {
        void WithButterfly(IServiceCollection services) => services
            .AddOcelot()
            .AddButterfly(option =>
            {
                option.CollectorUrl = DownstreamUrl(butterflyPort); // this is the url that the butterfly collector server is running on...
                option.Service = "Ocelot";
            });
        GivenOcelotIsRunning(WithButterfly);
    }

    private void GivenFakeButterfly(int port)
    {
        Task Map(HttpContext context)
        {
            _butterflyCalled++;
            return context.Response.WriteAsync("OK...");
        }
        handler.GivenThereIsAServiceRunningOn(port, Map);
    }

    private string GivenServiceIsRunning(int port, string basePath, HttpStatusCode statusCode, string responseBody, int butterflyPort, string serviceName)
    {
        string downstreamPath = string.Empty;
        void WithButterfly(IServiceCollection services)
        {
            services.AddButterfly(option =>
            {
                option.CollectorUrl = DownstreamUrl(butterflyPort);
                option.Service = serviceName;
                option.IgnoredRoutesRegexPatterns = [];
            });
        }
        Task MapStatusAndPath(HttpContext context)
        {
            downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(oK ? responseBody : "downstream path didnt match base path");
        }
        handler.GivenThereIsAServiceRunningOn(DownstreamUrl(port), basePath, WithButterfly, MapStatusAndPath);
        return downstreamPath;
    }
}
