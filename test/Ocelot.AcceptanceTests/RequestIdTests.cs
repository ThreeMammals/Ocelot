using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class RequestIdTests : Steps
{
    public const string RequestIdKey = "Oc-RequestId";
    private readonly ServiceHandler _serviceHandler;

    public RequestIdTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    [Fact]
    public void Should_use_default_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    RequestIdKey = RequestIdKey,
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheRequestIdIsReturned())
            .BDDfy();
    }

    [Fact]
    public void Should_use_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
        };

        var requestId = Guid.NewGuid().ToString();
        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayWithRequestId("/", requestId))
            .Then(x => ThenTheRequestIdIsReturned(requestId))
            .BDDfy();
    }

    [Fact]
    public void Should_use_global_request_id_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RequestIdKey = RequestIdKey,
            },
        };

        var requestId = Guid.NewGuid().ToString();
        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayWithRequestId("/", requestId))
            .Then(x => ThenTheRequestIdIsReturned(requestId))
            .BDDfy();
    }

    [Fact]
    public void Should_use_global_request_id_create_and_forward()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RequestIdKey = RequestIdKey,
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheRequestIdIsReturned())
            .BDDfy();
    }

    private async Task WhenIGetUrlOnTheApiGatewayWithRequestId(string url, string requestId)
    {
        _ocelotClient.DefaultRequestHeaders.TryAddWithoutValidation(RequestIdKey, requestId);
        _response = await _ocelotClient.GetAsync(url);
    }

    private void GivenThereIsAServiceRunningOn(string url)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(url, context =>
        {
            context.Request.Headers.TryGetValue(RequestIdKey, out var requestId);
            context.Response.Headers[RequestIdKey] = requestId.First();
            return Task.CompletedTask;
        });
    }

    private void ThenTheRequestIdIsReturned()
        => _response.Headers.GetValues(RequestIdKey).First().ShouldNotBeNullOrEmpty();
    private void ThenTheRequestIdIsReturned(string expected)
        => _response.Headers.GetValues(RequestIdKey).First().ShouldBe(expected);

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }
}
