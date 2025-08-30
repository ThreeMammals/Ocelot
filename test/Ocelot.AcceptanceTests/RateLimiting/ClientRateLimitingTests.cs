using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using Ocelot.RateLimiting;

namespace Ocelot.AcceptanceTests.RateLimiting;

public sealed class ClientRateLimitingTests : RateLimitingSteps
{
    const int OK = (int)HttpStatusCode.OK;
    const int TooManyRequests = (int)HttpStatusCode.TooManyRequests;
    private int _counterOne;

    public ClientRateLimitingTests()
    {
    }

    [Fact]
    [Trait("Feat", "37")]
    public void Should_call_with_rate_limiting()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, null, null, new(), 3, "1s", 1); // periods are equal
        var configuration = GivenConfigurationWithRateLimitOptions(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/api/ClientRateLimit", 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/api/ClientRateLimit", 2))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/api/ClientRateLimit", 1))
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
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 2))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => GivenIWait(1000))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => GivenIWait(1000))
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .And(x => ThenTheResponseBodyShouldBe("4")) // total 4 OK responses
            .BDDfy();
    }

    private int _count = 0;
    private int Count() => ++_count;
    private string Url() => $"/ClientRateLimit/?{Count()}";

    private async Task WhenIGetUrlOnTheApiGatewayMultipleTimes(Func<string> urlDelegate, long times)
    {
        for (long i = 0; i < times; i++)
        {
            var url = urlDelegate.Invoke();
            await WhenIGetUrlOnTheApiGatewayMultipleTimes(url, 1);
        }
    }

    [Fact]
    [Trait("Feat", "37")]
    public async Task Should_call_middleware_with_white_list_client()
    {
        const int Limit = 3;
        const string ClientID = "ocelotclient1";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, null, null, whitelist: [ClientID], Limit, "3s", 2); // main period is greater than ban one
        var configuration = GivenConfigurationWithRateLimitOptions(route);
        GivenThereIsAServiceRunningOn(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        var responses = await WhenIGetUrlOnTheApiGatewayMultipleTimesWithRateLimitingByAHeader(
                            "/api/ClientRateLimit", Limit + 1,
                            route.RateLimitOptions.ClientIdHeader.IfEmpty(configuration.GlobalConfiguration.RateLimitOptions.ClientIdHeader),
                            ClientID);
        ThenTheStatusCodeShouldBeOK();
        responses.Length.ShouldBe(Limit + 1);
        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        var bodies = responses.Select(r => r.Content.ReadAsStringAsync().Result).ToList();
        bodies.Sum(int.Parse).ShouldBe(10); // n * (n + 1) / 2 -> 4*5/2 -> 20/2
        bodies.Sort();
        bodies.ForEach(body => int.Parse(body).ShouldBe(bodies.IndexOf(body) + 1)); // 1, 2, 3, 4
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
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())

            // main scenario
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, limit)) // 100 times to reach the limit
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .And(x => ThenTheResponseBodyShouldBe(route.RateLimitOptions.Limit.ToString())) // total 100 OK responses

            // extra scenario
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1)) // 101st request should fail
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => GivenIWait((int)TimeSpan.FromSeconds(periodTimespan).TotalMilliseconds)) // in 3 secs PeriodTimespan will elapse
            .When(x => x.WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1))
            .Then(x => ThenTheStatusCodeShouldBe(OK))
            .And(x => ThenTheResponseBodyShouldBe("101")) // total 101 OK responses
            .BDDfy();
    }
    
    [Theory]
    [Trait("Bug", "1305")]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_set_ratelimiting_headers_on_response_when_EnableHeaders_set_to(bool enableHeaders)
    {
        int port = PortFinder.GetRandomPort();
        var configuration = CreateConfigurationForCheckingHeaders(port, enableHeaders);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/ClientRateLimit"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/api/ClientRateLimit", 1))
            .Then(x => ThenRateLimitingHeadersExistInResponse(enableHeaders))
            .And(x => ThenRetryAfterHeaderExistsInResponse(false))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/api/ClientRateLimit", 2))
            .Then(x => ThenRateLimitingHeadersExistInResponse(enableHeaders))
            .And(x => ThenRetryAfterHeaderExistsInResponse(false))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/api/ClientRateLimit", 1))
            .Then(x => ThenRateLimitingHeadersExistInResponse(false))
            .And(x => ThenRetryAfterHeaderExistsInResponse(enableHeaders))
            .BDDfy();
    }

    private FileConfiguration CreateConfigurationForCheckingHeaders(int port, bool enableHeaders)
    {
        var route = GivenRoute(port, null, null, new(), 3, "100s", 1000.0D);
        var config = GivenConfiguration(route);
        config.GlobalConfiguration.RateLimitOptions = new FileGlobalRateLimitByHeaderRule()
        {
            EnableHeaders = enableHeaders,
            QuotaExceededMessage = "",
            HttpStatusCode = TooManyRequests,
        };
        return config;
    }

    private void ThenRateLimitingHeadersExistInResponse(bool headersExist)
    {
        response.Headers.Contains(RateLimitingHeaders.X_Rate_Limit_Limit).ShouldBe(headersExist);
        response.Headers.Contains(RateLimitingHeaders.X_Rate_Limit_Remaining).ShouldBe(headersExist);
        response.Headers.Contains(RateLimitingHeaders.X_Rate_Limit_Reset).ShouldBe(headersExist);
    }

    private void ThenRetryAfterHeaderExistsInResponse(bool headersExist)
        => response.Headers.Contains(HeaderNames.RetryAfter).ShouldBe(headersExist);

    protected override void GivenThereIsAServiceRunningOn(int port, string basePath)
    {
        Task MapOK(HttpContext context)
        {
            int count = Interlocked.Increment(ref _counterOne); // _counterOne++;
            context.Response.StatusCode = OK;
            return context.Response.WriteAsync(count.ToString());
        }
        handler.GivenThereIsAServiceRunningOn(port, basePath, MapOK);
    }

    private FileRoute GivenRoute(int port, string downstream, string upstream, List<string> whitelist, long limit, string period, double periodTimespan) => new()
    {
        DownstreamPathTemplate = downstream ?? "/api/ClientRateLimit",
        DownstreamHostAndPorts = new()
        {
            Localhost(port),
        },
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = upstream ?? "/api/ClientRateLimit",
        UpstreamHttpMethod = [HttpMethods.Get],
        RequestIdKey = "Oc-RequestId",
        RateLimitOptions = new()
        {
            EnableRateLimiting = true,
            ClientWhitelist = whitelist ?? ["ocelotclient1"],
            Limit = limit,
            Period = period ?? "1s",
            PeriodTimespan = periodTimespan,
        },
    };

    private FileConfiguration GivenConfigurationWithRateLimitOptions(params FileRoute[] routes)
    {
        var config = GivenConfiguration(routes);
        config.GlobalConfiguration = new()
        {
            RateLimitOptions = new()
            {
                ClientIdHeader = "ClientId",
                QuotaExceededMessage = "Exceeding!",
                RateLimitCounterPrefix = "ABC",
                HttpStatusCode = TooManyRequests, // 429
            },
            RequestIdKey = "OcelotClientRequest",
        };
        return config;
    }
}
