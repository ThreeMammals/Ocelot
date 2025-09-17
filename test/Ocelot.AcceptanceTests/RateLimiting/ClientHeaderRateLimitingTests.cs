using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration;
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
        ThenTheResponseHeaderExists(HeaderNames.RetryAfter, false);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 2);
        ThenTheStatusCodeShouldBeOK();
        ThenRateLimitingHeadersExistInResponse(enableHeaders);
        ThenTheResponseHeaderExists(HeaderNames.RetryAfter, false);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBe(TooManyRequests);
        ThenRateLimitingHeadersExistInResponse(false);
        ThenTheResponseHeaderExists(HeaderNames.RetryAfter, enableHeaders);
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
        ThenTheResponseHeaderExists(HeaderNames.RetryAfter).ShouldBe("-1");
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "1915")] // https://github.com/ThreeMammals/Ocelot/issues/1915
    public async Task Should_apply_global_options_when_there_are_no_route_opts()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.RateLimitOptions = null; // !!!
        var configuration = GivenConfiguration(route);
        var global = configuration.GlobalConfiguration.RateLimitOptions;
        global.Limit = 3;
        global.Period = "1s";
        global.Wait = "500ms";
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 3); // 3
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1); // 4, exceeding
        ThenTheStatusCodeShouldBe(TooManyRequests);
        int halfOfWaitWindow = 250;
        GivenIWait(halfOfWaitWindow);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1); // 5
        ThenTheStatusCodeShouldBe(TooManyRequests);
        ThenTheResponseBodyShouldBe("Exceeding!");
        var retryAfter = ThenTheResponseHeaderExists(HeaderNames.RetryAfter);
        retryAfter.ShouldStartWith("0.2"); // 0.2xx
        var seconds = double.Parse(retryAfter);
        int theRestOfMilliseconds = (int)(1000 * seconds);
        theRestOfMilliseconds.ShouldBeInRange(200, halfOfWaitWindow);
        GivenIWait(halfOfWaitWindow); // the end of wait period
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1); // 1, new counting period has started
        ThenTheStatusCodeShouldBeOK();
    }

    [Fact]
    [Trait("Feat", "1229")] // https://github.com/ThreeMammals/Ocelot/issues/1229
    public async Task Should_apply_group_global_options_when_route_opts_has_a_key()
    {
        // 1st route
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, upstream: "/rateUnlimited");
        route.RateLimitOptions = null; // 1st route is not limited
        route.Key = null; // 1st route is not in the global group

        // 2nd route
        var port2 = PortFinder.GetRandomPort();
        var route2 = GivenRoute(port2,
            downstream: "/api/ClientRateLimit2?count={count}", upstream: "/rateLimited/?{count}");
        route2.RateLimitOptions = null; // 2nd route opts will be applied from global ones
        route2.Key = "R2"; // 2nd route is in the group

        var configuration = GivenConfiguration(route, route2);
        var global = configuration.GlobalConfiguration.RateLimitOptions;
        global.RouteKeys = ["R2"];
        global.Limit = 3;
        global.Period = "1s";
        global.Wait = "500ms";

        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAServiceRunningOn(port2, "/api/ClientRateLimit2", MapOK);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        // Make requests to the 1st unlimited route
        var responses = await WhenIGetUrlOnTheApiGatewayMultipleTimes("/rateUnlimited", (int)global.Limit + 1);
        ThenTheStatusCodeShouldBeOK();
        responses.Length.ShouldBe((int)global.Limit + 1);
        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        var bodies = responses.Select(r => r.Content.ReadAsStringAsync().Result).ToList();
        bodies.ForEach(b => b.ShouldBe(Body()));

        // Make requests to the 2nd rate-limited route
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/rateLimited/", 3); // 3
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/rateLimited/", 1); // 4, exceeding
        ThenTheStatusCodeShouldBe(TooManyRequests);
        int halfOfWaitWindow = 250;
        GivenIWait(halfOfWaitWindow);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/rateLimited/", 1); // 5
        ThenTheStatusCodeShouldBe(TooManyRequests);
        ThenTheResponseBodyShouldBe("Exceeding!");
        var retryAfter = ThenTheResponseHeaderExists(HeaderNames.RetryAfter);
        retryAfter.ShouldStartWith("0.2"); // 0.2xx
        var seconds = double.Parse(retryAfter);
        int theRestOfMilliseconds = (int)(1000 * seconds);
        theRestOfMilliseconds.ShouldBeInRange(200, halfOfWaitWindow);
        GivenIWait(halfOfWaitWindow); // the end of wait period
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/rateLimited/", 1); // 1, new counting period has started
        ThenTheStatusCodeShouldBeOK();
    }

    [Fact]
    [Trait("PR", "2294")]
    public async Task Should_rate_limit_using_sliding_period_without_wait_period()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        route.RateLimitOptions = new(route.RateLimitOptions)
        {
            Limit = 3,
            Period = "1s",
            PeriodTimespan = null,
            Wait = string.Empty, // No wait window -> sliding period in fixed window aka Period is 1s
        }; // rule -> 3/1s/w0
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 3);
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
        ThenTheStatusCodeShouldBe(TooManyRequests);
        ThenTheResponseBodyShouldBe("Exceeding!");
        var retryAfter = ThenTheResponseHeaderExists(HeaderNames.RetryAfter);
        retryAfter.ShouldStartWith("0.9"); // 0.9xx
        int theRestOfMilliseconds = (int)(1000 * double.Parse(retryAfter));
        theRestOfMilliseconds.ShouldBeGreaterThan(900);

        // Mutual behavior arises from test instability, which is sensitive to consumed CPU resources and thread synchronization in CI/CD environments
        int slidingPeriodEndsInMs = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" // check GitHub CI-CD context
            ? (int)RateLimitRule.ParseTimespan(route.RateLimitOptions.Period).TotalMilliseconds // not strict requirement for CI-CD, ensure the test is stable
            : theRestOfMilliseconds; // otherwise it is strict in local dev env, but somethimes the test fails :D
        GivenIWait(slidingPeriodEndsInMs); // the end of sliding period

        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1); // 1, new counting period has started
        ThenTheStatusCodeShouldBeOK();
        /*
        this.Given(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 3))
            .Then(x => ThenTheStatusCodeShouldBeOK())
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1))
            .Then(x => ThenTheStatusCodeShouldBe(TooManyRequests))
            .And(x => ThenTheResponseBodyShouldBe("Exceeding!"))
            .And(x => ThenTheResponseHeaderExists(HeaderNames.RetryAfter))
            .And(x => ThenRetryAfterIs())
            .Given(x => GivenIWait(_theRestOfMilliseconds))
            .When(x => WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1))
            .Then(x => ThenTheStatusCodeShouldBeOK())
            .BDDfy();
        var isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
        Assert.True(isGitHubActions, "This test should run inside GitHub Actions.");
        */
    }

    //private string _retryAfter;
    //private int _theRestOfMilliseconds;
    //void ThenRetryAfterIs()
    //{
    //    _retryAfter = ThenTheResponseHeaderExists(HeaderNames.RetryAfter);
    //    _retryAfter.ShouldStartWith("0.9"); // 0.9xx
    //    _theRestOfMilliseconds = (int)(1000 * double.Parse(_retryAfter));
    //    _theRestOfMilliseconds.ShouldBeGreaterThan(900);
    //}
    private void ThenRateLimitingHeadersExistInResponse(bool headersExist)
    {
        response.Headers.Contains(RateLimitingHeaders.X_RateLimit_Limit).ShouldBe(headersExist);
        response.Headers.Contains(RateLimitingHeaders.X_RateLimit_Remaining).ShouldBe(headersExist);
        response.Headers.Contains(RateLimitingHeaders.X_RateLimit_Reset).ShouldBe(headersExist);
    }

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
