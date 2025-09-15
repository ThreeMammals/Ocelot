using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using Ocelot.RateLimiting;

namespace Ocelot.AcceptanceTests.RateLimiting;

public sealed class ClientHeaderRateLimitingTests : RateLimitingSteps
{
    const int OK = (int)HttpStatusCode.OK;
    const int TooManyRequests = (int)HttpStatusCode.TooManyRequests;
    private int _counter;

    public ClientHeaderRateLimitingTests()
    { }

    [Fact]
    [Trait("Feat", "37")]
    public async Task Should_call_with_rate_limiting()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, limit: 3, period: "1s", periodTimespan: 1); // -> 3/1s/w1s, so, periods are equal
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 2);
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBe(TooManyRequests);
    }

    [Fact]
    [Trait("Feat", "37")]
    public async Task Should_wait_for_period_timespan_to_elapse_before_making_next_request()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port,
            downstream: "/api/ClientRateLimit?count={count}", upstream: "/ClientRateLimit/?{count}",
            limit: 3, period: "1s", periodTimespan: 1); // -> 3/1s/w1s
        var configuration = GivenConfiguration(route);
        _counter = 0;
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1);
        ThenTheStatusCodeShouldBeOK();
        GivenIWait(50);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1); // 2
        ThenTheStatusCodeShouldBeOK();
        GivenIWait(50);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1); // 3
        ThenTheStatusCodeShouldBeOK();
        GivenIWait(50);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1); // 4, exceeded with 150ms shift
        ThenTheStatusCodeShouldBe(TooManyRequests);
        GivenIWait(500); // half of wait window
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1); // 5
        ThenTheStatusCodeShouldBe(TooManyRequests);
        GivenIWait(500 + 5); // wait window has elapsed
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1); // 6->1
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBodyShouldBe("4"); // total 4 OK responses
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
        var route = GivenRoute(port, whitelist: [ClientID],
            limit: Limit, period: "3s", periodTimespan: 2); // main period is greater than wait window one
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        var responses = await WhenIGetUrlOnTheApiGatewayMultipleTimesWithRateLimitingByAHeader("/ClientRateLimit",
                            Limit + 1,
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
    public async Task StatusShouldNotBeEqualTo429_PeriodTimespanValueIsGreaterThanPeriod()
    {
        _counter = 0;

        // Bug scenario
        const string period = "1s";
        const double periodTimespan = /*30*/3; // but decrease 30 to 3 secs, "no wasting time" life hack
        const long limit = 100L;

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/api/ClientRateLimit?count={count}", "/ClientRateLimit/?{count}", new(),
            limit, period, periodTimespan); // bug scenario, adapted
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        // main scenario
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, limit); // 100 times to reach the limit
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBodyShouldBe(route.RateLimitOptions.Limit.ToString()); // total 100 OK responses

        // extra scenario
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1); // 101st request should fail
        ThenTheStatusCodeShouldBe(TooManyRequests);
        GivenIWait((int)TimeSpan.FromSeconds(periodTimespan).TotalMilliseconds); // in 3 secs Wait will elapse
        await WhenIGetUrlOnTheApiGatewayMultipleTimes(Url, 1);
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBodyShouldBe("101"); // total 101 OK responses
    }

    [Theory]
    [Trait("Bug", "1305")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_set_ratelimiting_headers_on_response_when_EnableHeaders_set_to(bool enableHeaders)
    {
        int port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, limit: 3, period: "100s", periodTimespan: 1000.0D); // 3/100s/w1000.00s
        route.RateLimitOptions.EnableHeaders = enableHeaders;
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBeOK();
        ThenRateLimitingHeadersExistInResponse(enableHeaders);
        ThenRetryAfterHeaderExistsInResponse(false);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 2);
        ThenTheStatusCodeShouldBeOK();
        ThenRateLimitingHeadersExistInResponse(enableHeaders);
        ThenRetryAfterHeaderExistsInResponse(false);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBe(TooManyRequests);
        ThenRateLimitingHeadersExistInResponse(false);
        ThenRetryAfterHeaderExistsInResponse(enableHeaders);
    }

    [Fact]
    [Trait("Feat", "37")]
    [Trait("Feat", "585")]
    [Trait("PR", "2294")]
    public async Task Should_block_unknown_clients_by_writing_warning_to_body_with_503_status()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, limit: 3, period: "1s", periodTimespan: 1); // -> 3/1s/w1s
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayMultipleTimesWithRateLimitingByAHeader("/ClientRateLimit", 1, "bla-bla-header", "spy");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        ThenTheResponseBodyShouldBe("Rate limiting client could not be identified for the route '/ClientRateLimit' due to a missing or unknown client ID header required by rule '3/1s/w1s'!");
        ThenRetryAfterHeaderExistsInResponse(true);
        ThenTheResponseHeaderIs(HeaderNames.RetryAfter, "-1");
    }

    [Fact(Skip = "TODO: To be developed")]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "1915")] // https://github.com/ThreeMammals/Ocelot/issues/1915
    public async Task ShouldApplyGlobalRateLimitingOptionsIfThereAreNoRouteOpts()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.RateLimitOptions = null; // !!!
        var configuration = GivenConfiguration(route);
        var global = configuration.GlobalConfiguration.RateLimitOptions;
        global.Limit = 3;
        global.Period = "100.ms";
        global.PeriodTimespan = 0.5D;
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 2);
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        /*ThenTheStatusCodeShouldBe(TooManyRequests);*/
        int halfOfWaitWindow = (int)(1000D * global.PeriodTimespan.Value / 2D);
        await Task.Delay(halfOfWaitWindow);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBe(TooManyRequests);
        ThenTheResponseBodyShouldBe("Exceeding!");
        response.Headers.Contains(HeaderNames.RetryAfter).ShouldBeTrue();
        var ra = ThenTheResponseHeaderExists(HeaderNames.RetryAfter);
        ThenTheResponseHeaderIs(HeaderNames.RetryAfter, "000");
        await Task.Delay(halfOfWaitWindow + 50);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBeOK();
    }

    private void ThenRateLimitingHeadersExistInResponse(bool headersExist)
    {
        response.Headers.Contains(RateLimitingHeaders.X_RateLimit_Limit).ShouldBe(headersExist);
        response.Headers.Contains(RateLimitingHeaders.X_RateLimit_Remaining).ShouldBe(headersExist);
        response.Headers.Contains(RateLimitingHeaders.X_RateLimit_Reset).ShouldBe(headersExist);
    }

    private void ThenRetryAfterHeaderExistsInResponse(bool headersExist)
        => response.Headers.Contains(HeaderNames.RetryAfter).ShouldBe(headersExist);

    protected override Task MapOK(HttpContext context)
    {
        int count = Interlocked.Increment(ref _counter); // thread-safe analog of _counter++
        context.Response.StatusCode = OK;
        return context.Response.WriteAsync(count.ToString());
    }

    private FileRoute GivenRoute(int port, string downstream = null, string upstream = null,
        List<string> whitelist = null, long? limit = null, string period = null, double? periodTimespan = null)
    {
        var route = base.GivenRoute(port, upstream ?? "/ClientRateLimit", downstream ?? "/api/ClientRateLimit");
        route.RequestIdKey = "Oc-RequestId";
        route.RateLimitOptions = new()
        {
            //EnableRateLimiting = true,
            ClientWhitelist = whitelist, //?? ["ocelotclient1"],
            Limit = limit ?? 3,
            Period = period.IfEmpty("1s"),
            PeriodTimespan = periodTimespan ?? 1D,
        };
        return route;
    }

    private FileConfiguration GivenConfiguration(params FileRoute[] routes)
    {
        var config = base.GivenConfiguration(routes);
        config.GlobalConfiguration.RateLimitOptions = new()
        {
            ClientIdHeader = "ClientId",
            QuotaExceededMessage = "Exceeding!",
            RateLimitCounterPrefix = "ABC",
            HttpStatusCode = TooManyRequests, // 429
        };
        config.GlobalConfiguration.RequestIdKey = "OcelotClientRequest";
        return config;
    }
}
