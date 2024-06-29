using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.RateLimiting;

public sealed class ClientRateLimitingTests : Steps, IDisposable
{
    const int OK = (int)HttpStatusCode.OK;
    const int TooManyRequests = (int)HttpStatusCode.TooManyRequests;

    private int _counterOne;
    private readonly ServiceHandler _serviceHandler;

    public ClientRateLimitingTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    [Trait("Feat", "37")]
    public void Should_call_with_rate_limiting()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, null, null, new(), 3, "1s", 1); // periods are equal
        var configuration = GivenConfigurationWithRateLimitOptions(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 2))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "37")]
    public void Should_wait_for_period_timespan_to_elapse_before_making_next_request()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/api/ClientRateLimit?count={count}", "/ClientRateLimit/?{count}", new(), 3, "1s", 2);
        var configuration = GivenConfigurationWithRateLimitOptions(route);
        _counterOne = 0;
        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, 2))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => GivenIWait(1000))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => GivenIWait(1000))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .And(x => ThenTheResponseBodyShouldBe("4")) // total 4 OK responses
            .BDDfy();
    }

    private int _count = 0;
    private int Count() => ++_count;
    private string Url() => $"/ClientRateLimit/?{Count()}";

    private void WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Func<string> urlDelegate, long times)
    {
        for (long i = 0; i < times; i++)
        {
            var url = urlDelegate.Invoke();
            WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(url, 1);
        }
    }

    [Fact]
    [Trait("Feat", "37")]
    public void Should_call_middleware_with_white_list_client()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, null, null, whitelist: new() { "ocelotclient1" }, 3, "3s", 2); // main period is greater than ban one
        var configuration = GivenConfigurationWithRateLimitOptions(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 4))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void StatusShouldNotBeEqualTo429_PeriodTimespanValueIsGreaterThanPeriod()
    {
        _counterOne = 0;

        // Bug scenario
        const string period = "1s";
        const double periodTimespan = /*30*/3; // but decrease 30 to 3 secs, "no wasting time" life hack
        const long limit = 100L;

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/api/ClientRateLimit?count={count}", "/ClientRateLimit/?{count}", new(),
            limit, period, periodTimespan); // bug scenario, adapted
        var configuration = GivenConfigurationWithRateLimitOptions(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())

            // main scenario
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, route.RateLimitOptions.Limit)) // 100 times to reach the limit
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .And(x => ThenTheResponseBodyShouldBe(route.RateLimitOptions.Limit.ToString())) // total 100 OK responses

            // extra scenario
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, 1)) // 101st request should fail
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => GivenIWait((int)TimeSpan.FromSeconds(route.RateLimitOptions.PeriodTimespan).TotalMilliseconds)) // in 3 secs PeriodTimespan will elapse
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .And(x => ThenTheResponseBodyShouldBe("101")) // total 101 OK responses
            .BDDfy();
    }

    [Fact]
    public void should_set_ratelimiting_headers_on_response_when_DisableRateLimitHeaders_set_to_false()
    {
        int port = PortFinder.GetRandomPort();

        var configuration = CreateConfigurationForCheckingHeaders(port, false);

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => ThenRateLimitingHeadersExistInResponse(true))
            .And(x => ThenRetryAfterHeaderExistsInResponse(false))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 2))
            .Then(x => ThenRateLimitingHeadersExistInResponse(true))
            .And(x => ThenRetryAfterHeaderExistsInResponse(false))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => ThenRateLimitingHeadersExistInResponse(false))
            .And(x => ThenRetryAfterHeaderExistsInResponse(true))
            .BDDfy();
    }

    [Fact]
    public void should_not_set_ratelimiting_headers_on_response_when_DisableRateLimitHeaders_set_to_true()
    {
        int port = PortFinder.GetRandomPort();

        var configuration = CreateConfigurationForCheckingHeaders(port, true);

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => ThenRateLimitingHeadersExistInResponse(false))
            .And(x => ThenRetryAfterHeaderExistsInResponse(false))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 2))
            .Then(x => ThenRateLimitingHeadersExistInResponse(false))
            .And(x => ThenRetryAfterHeaderExistsInResponse(false))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimesForRateLimit("/api/ClientRateLimit", 1))
            .Then(x => ThenRateLimitingHeadersExistInResponse(false))
            .And(x => ThenRetryAfterHeaderExistsInResponse(false))
            .BDDfy();
    }

    private FileConfiguration CreateConfigurationForCheckingHeaders(int port, bool disableRateLimitHeaders)
    {
        return new FileConfiguration
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
                        RateLimitOptions = new FileRateLimitRule()
                        {
                            EnableRateLimiting = true,
                            ClientWhitelist = new List<string>(),
                            Limit = 3,
                            Period = "100s",
                            PeriodTimespan = 1000,
                        },
                    },
                },
            GlobalConfiguration = new FileGlobalConfiguration()
            {
                RateLimitOptions = new FileRateLimitOptions()
                {
                    DisableRateLimitHeaders = disableRateLimitHeaders,
                    QuotaExceededMessage = "",
                    HttpStatusCode = 428,
                },
            },
        };
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, context =>
        {
            _counterOne++;
            context.Response.StatusCode = OK;
            context.Response.WriteAsync(_counterOne.ToString());
            return Task.CompletedTask;
        });
    }

    private FileRoute GivenRoute(int port, string downstream, string upstream, List<string> whitelist, long limit, string period, double periodTimespan) => new()
    {
        DownstreamPathTemplate = downstream ?? "/api/ClientRateLimit",
        DownstreamHostAndPorts = new()
        {
            new("localhost", port),
        },
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = upstream ?? "/api/ClientRateLimit",
        UpstreamHttpMethod = new() { HttpMethods.Get },
        RequestIdKey = RequestIdKey,
        RateLimitOptions = new FileRateLimitRule
        {
            EnableRateLimiting = true,
            ClientWhitelist = whitelist ?? new() { "ocelotclient1" },
            Limit = limit,
            Period = period ?? "1s",
            PeriodTimespan = periodTimespan,
        },
    };

    private static FileConfiguration GivenConfigurationWithRateLimitOptions(params FileRoute[] routes)
    {
        var config = GivenConfiguration(routes);
        config.GlobalConfiguration = new()
        {
            RateLimitOptions = new()
            {
                ClientIdHeader = "ClientId",
                DisableRateLimitHeaders = false,
                QuotaExceededMessage = "Exceeding!",
                RateLimitCounterPrefix = "ABC",
                HttpStatusCode = TooManyRequests, // 429
            },
            RequestIdKey = "OcelotClientRequest",
        };
        return config;
    }

    private void ThenRateLimitingHeadersExistInResponse(bool headersExist)
    {
        _response.Headers.Contains("X-Rate-Limit-Limit").ShouldBe(headersExist);
        _response.Headers.Contains("X-Rate-Limit-Remaining").ShouldBe(headersExist);
        _response.Headers.Contains("X-Rate-Limit-Reset").ShouldBe(headersExist);
    }

    private void ThenRetryAfterHeaderExistsInResponse(bool headersExist)
    {
        _response.Headers.Contains(HeaderNames.RetryAfter).ShouldBe(headersExist);
    }
}
