using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.RateLimiting;

public sealed class ClientRateLimitingTests : IDisposable
{
    const int OK = (int)HttpStatusCode.OK;
    const int TooManyRequests = (int)HttpStatusCode.TooManyRequests;

    private readonly Steps _steps;
    private int _counterOne;
    private readonly ServiceHandler _serviceHandler;

    public ClientRateLimitingTests()
    {
        _serviceHandler = new ServiceHandler();
        _steps = new Steps();
    }

    public void Dispose()
    {
        _steps.Dispose();
    }

    [Fact]
    public void Should_call_withratelimiting()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/api/ClientRateLimit",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/api/ClientRateLimit",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    RequestIdKey = _steps.RequestIdKey,
                    RateLimitOptions = new FileRateLimitRule
                    {
                        EnableRateLimiting = true,
                        ClientWhitelist = new List<string>(),
                        Limit = 3,
                        Period = "1s",
                        PeriodTimespan = 1000,
                    },
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RateLimitOptions = new FileRateLimitOptions
                {
                    ClientIdHeader = "ClientId",
                    DisableRateLimitHeaders = false,
                    QuotaExceededMessage = string.Empty,
                    RateLimitCounterPrefix = string.Empty,
                    HttpStatusCode = TooManyRequests,
                },
                RequestIdKey = "oceclientrequest",
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/ClientRateLimit"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(OK))
            .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 2))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(OK))
            .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(TooManyRequests))
            .BDDfy();
    }

    [Fact]
    public void Should_wait_for_period_timespan_to_elapse_before_making_next_request()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/api/ClientRateLimit?count={count}",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/ClientRateLimit/?{count}",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    RequestIdKey = _steps.RequestIdKey,

                    RateLimitOptions = new FileRateLimitRule
                    {
                        EnableRateLimiting = true,
                        ClientWhitelist = new List<string>(),
                        Limit = 3,
                        Period = "1s",
                        PeriodTimespan = 2, // seconds
                    },
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RateLimitOptions = new FileRateLimitOptions
                {
                    ClientIdHeader = "ClientId",
                    DisableRateLimitHeaders = false,
                    QuotaExceededMessage = string.Empty,
                    RateLimitCounterPrefix = string.Empty,
                    HttpStatusCode = TooManyRequests, // 429
                },
                RequestIdKey = "oceclientrequest",
            },
        };
        _counterOne = 0;
        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/ClientRateLimit"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(() => $"/ClientRateLimit/?{Count()}", 1))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(OK))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(() => $"/ClientRateLimit/?{Count()}", 2))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(OK))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(() => $"/ClientRateLimit/?{Count()}", 1))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => _steps.GivenIWait(1000))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(() => $"/ClientRateLimit/?{Count()}", 1))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => _steps.GivenIWait(1000))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(() => $"/ClientRateLimit/?{Count()}", 1))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("4")) // total 4 OK responses
            .BDDfy();
    }

    private int _count = 0;
    private int Count() => ++_count;
    private void WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Func<string> urlDelegate, int times)
    {
        for (int i = 0; i < times; i++)
        {
            var url = urlDelegate.Invoke();
            _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(url, 1);
        }
    }

    [Fact]
    public void Should_call_middleware_withWhitelistClient()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/api/ClientRateLimit",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/api/ClientRateLimit",
                    UpstreamHttpMethod = new List<string> { "Get" },
                    RequestIdKey = _steps.RequestIdKey,

                    RateLimitOptions = new FileRateLimitRule
                    {
                        EnableRateLimiting = true,
                        ClientWhitelist = new List<string> { "ocelotclient1"},
                        Limit = 3,
                        Period = "1s",
                        PeriodTimespan = 100,
                    },
                },
            },
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RateLimitOptions = new FileRateLimitOptions
                {
                    ClientIdHeader = "ClientId",
                    DisableRateLimitHeaders = false,
                    QuotaExceededMessage = string.Empty,
                    RateLimitCounterPrefix = string.Empty,
                },
                RequestIdKey = "oceclientrequest",
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/ClientRateLimit"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 4))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(OK))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, context =>
        {
            _counterOne++;
            context.Response.StatusCode = 200;
            context.Response.WriteAsync(_counterOne.ToString());
            return Task.CompletedTask;
        });
    }
}
