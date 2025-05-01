using Butterfly.Client.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Tracing.Butterfly;
using Xunit.Abstractions;

namespace Ocelot.AcceptanceTests;

public sealed class ButterflyTracingTests : Steps
{
    private IWebHost _serviceOneBuilder;
    private IWebHost _serviceTwoBuilder;
    private IWebHost _fakeButterfly;
    private string _downstreamPathOne;
    private string _downstreamPathTwo;
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
        var butterflyUrl = $"http://localhost:{butterflyPort}";

        this.Given(x => GivenFakeButterfly(butterflyUrl))
            .And(x => GivenServiceOneIsRunning($"http://localhost:{port1}", "/api/values", 200, "Hello from Laura", butterflyUrl))
            .And(x => GivenServiceTwoIsRunning($"http://localhost:{port2}", "/api/values", 200, "Hello from Tom", butterflyUrl))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingButterfly(butterflyUrl))
            .When(x => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(x => WhenIGetUrlOnTheApiGateway("/api002/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Tom"))
            .BDDfy();

        var commandOnAllStateMachines = Wait.WaitFor(10000).Until(() => _butterflyCalled >= 4);

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
        var butterflyUrl = $"http://localhost:{butterflyPort}";

        this.Given(x => GivenFakeButterfly(butterflyUrl))
            .And(x => GivenServiceOneIsRunning($"http://localhost:{port}", "/api/values", 200, "Hello from Laura", butterflyUrl))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningUsingButterfly(butterflyUrl))
            .When(x => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheTraceHeaderIsSet("Trace-Id"))
            .And(x => ThenTheResponseHeaderIs("Tom", "Laura"))
            .BDDfy();
    }

    private void GivenOcelotIsRunningUsingButterfly(string butterflyUrl)
    {
        var builder = TestHostBuilder.Create()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", true, false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, false);
                config.AddJsonFile(_ocelotConfigFileName, true, false);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices(s =>
            {
                s.AddOcelot()
                    .AddButterfly(option =>
                    {
                        //this is the url that the butterfly collector server is running on...
                        option.CollectorUrl = butterflyUrl;
                        option.Service = "Ocelot";
                    });
            })
            .Configure(async app =>
            {
                app.Use(async (_, next) => { await next.Invoke(); });
                await app.UseOcelot();
            });

        _ocelotServer = new TestServer(builder);
        _ocelotClient = _ocelotServer.CreateClient();
    }

    private void GivenServiceOneIsRunning(string baseUrl, string basePath, int statusCode, string responseBody, string butterflyUrl)
    {
        _serviceOneBuilder = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .ConfigureServices(services =>
            {
                services.AddButterfly(option =>
                {
                    option.CollectorUrl = butterflyUrl;
                    option.Service = "Service One";
                    option.IgnoredRoutesRegexPatterns = Array.Empty<string>();
                });
            })
            .Configure(app =>
            {
                app.UsePathBase(basePath);
                app.Run(async context =>
                {
                    _downstreamPathOne = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                    if (_downstreamPathOne != basePath)
                    {
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync("downstream path didnt match base path");
                    }
                    else
                    {
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync(responseBody);
                    }
                });
            })
            .Build();

        _serviceOneBuilder.Start();
    }

    private void GivenFakeButterfly(string baseUrl)
    {
        _fakeButterfly = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    _butterflyCalled++;
                    await context.Response.WriteAsync("OK...");
                });
            })
            .Build();

        _fakeButterfly.Start();
    }

    private void GivenServiceTwoIsRunning(string baseUrl, string basePath, int statusCode, string responseBody, string butterflyUrl)
    {
        _serviceTwoBuilder = TestHostBuilder.Create()
            .UseUrls(baseUrl)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .ConfigureServices(services =>
            {
                services.AddButterfly(option =>
                {
                    option.CollectorUrl = butterflyUrl;
                    option.Service = "Service Two";
                    option.IgnoredRoutesRegexPatterns = Array.Empty<string>();
                });
            })
            .Configure(app =>
            {
                app.UsePathBase(basePath);
                app.Run(async context =>
                {
                    _downstreamPathTwo = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

                    if (_downstreamPathTwo != basePath)
                    {
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync("downstream path didnt match base path");
                    }
                    else
                    {
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync(responseBody);
                    }
                });
            })
            .Build();

        _serviceTwoBuilder.Start();
    }

    public override void Dispose()
    {
        _serviceOneBuilder?.Dispose();
        _serviceTwoBuilder?.Dispose();
        _fakeButterfly?.Dispose();
        base.Dispose();
    }
}
