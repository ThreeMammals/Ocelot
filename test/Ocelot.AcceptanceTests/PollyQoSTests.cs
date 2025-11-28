using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Configuration;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Polly;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ocelot.AcceptanceTests;

[Trait("Feat", "23")] // https://github.com/ThreeMammals/Ocelot/issues/23
[Trait("Feat", "39")] // https://github.com/ThreeMammals/Ocelot/pull/39
public sealed class PollyQoSTests : TimeoutTestsBase
{
    private FileRoute GivenRoute(int port, QoSOptions options, string httpMethod = null, string upstream = null)
    {
        var route = GivenRoute(port, upstream, null);
        route.UpstreamHttpMethod = [ httpMethod ?? HttpMethods.Get ];
        route.QoSOptions = new(options);
        return route;
    }

    [Fact]
    [Trait("Feat", "318")] // https://github.com/ThreeMammals/Ocelot/issues/318
    [Trait("PR", "319")] // https://github.com/ThreeMammals/Ocelot/pull/319
    public async Task Should_not_timeout()
    {
        var qos = new QoSOptions()
        {
            BreakDuration = 500,
            MinimumThroughput = 10,
            FailureRatio = 0.5,
            SamplingDuration = 5,
            Timeout = 1000, // !!!
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos, HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, timeout: 10); // !!!
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();
        await WhenIPostUrlOnTheApiGateway("/", "postContent");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Feat", "318")] // https://github.com/ThreeMammals/Ocelot/issues/318
    [Trait("PR", "319")] // https://github.com/ThreeMammals/Ocelot/pull/319
    public async Task Should_timeout()
    {
        var qos = new QoSOptions(1000); // timeout
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos, HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, timeout: 2100);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();
        await WhenIPostUrlOnTheApiGateway("/", "postContent");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    [Trait("Bug", "1550")] // https://github.com/ThreeMammals/Ocelot/issues/1550
    [Trait("Bug", "1706")] // https://github.com/ThreeMammals/Ocelot/issues/1706
    [Trait("PR", "1753")] // https://github.com/ThreeMammals/Ocelot/pull/1753
    public async Task Should_open_circuit_breaker_after_two_exceptions()
    {
        var qos = new QoSOptions(2, 1000)
        {
            Timeout = 100_000, // infinite -> actually no timeout
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();
        for (int i = 0; i < qos.MinimumThroughput.Value; i++)
        {
            await WhenIGetUrlOnTheApiGateway("/");
            ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError);
        }
        await WhenIGetUrlOnTheApiGateway("/"); // opened
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // Polly status
    }

    [Fact]
    [Trait("Bug", "2085")] // https://github.com/ThreeMammals/Ocelot/issues/2085
    public async Task Should_open_circuit_breaker_for_DefaultBreakDuration()
    {
        int invalidDuration = CircuitBreakerStrategy.LowBreakDuration; // valid value must be >500ms, exact 500ms is invalid
        var qos = new QoSOptions(2, invalidDuration)
        {
            Timeout = 100_000,
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError);
        await WhenIGetUrlOnTheApiGateway("/"); // opened
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // Polly status
        GivenIWaitMilliseconds(CircuitBreakerStrategy.DefaultBreakDuration - 500); // BreakDuration is not elapsed
        await WhenIGetUrlOnTheApiGateway("/"); // still opened
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // still opened
        GivenThereIsABrokenServiceOnline(HttpStatusCode.NotFound);
        GivenIWaitMilliseconds(500); // BreakDuration should elapse now
        await WhenIGetUrlOnTheApiGateway("/"); // closed, service online
        ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound); // closed, service online
        ThenTheResponseBodyShouldBe(nameof(HttpStatusCode.NotFound));
    }

    private const string SkippingOnMacOS = "Skipping the test on MacOS platform: the test is stable in Linux and Windows only!";

    [SkippableFact]
    [Trait("PR", "39")] // https://github.com/ThreeMammals/Ocelot/pull/39
    public async Task Should_open_circuit_breaker_then_close()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), SkippingOnMacOS);
        var qos = new QoSOptions(2, CircuitBreakerStrategy.LowBreakDuration + 1) // 501
        {
            Timeout = 1000,
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        GivenThereIsAPossiblyBrokenServiceRunningOn(port, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        await WhenIGetUrlOnTheApiGateway("/"); // repeat same request because min MinimumThroughput is 2
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        GivenIWaitMilliseconds(3000); // qos.BreakDuration.Value
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
    }

    [SkippableFact]
    [Trait("PR", "39")] // https://github.com/ThreeMammals/Ocelot/pull/39
    public async Task Open_circuit_should_not_effect_different_route()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), SkippingOnMacOS);
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var qos1 = new QoSOptions(2, CircuitBreakerStrategy.LowBreakDuration + 1) // 501\
        {
            Timeout = 1000,
        };
        var route = GivenRoute(port1, qos1);
        var route2 = GivenRoute(port2, new(), null, "/working");
        var configuration = GivenConfiguration(route, route2);
        GivenThereIsAPossiblyBrokenServiceRunningOn(port1, "Hello from Laura");
        GivenThereIsAServiceRunningOn(port2, HttpStatusCode.OK, 0, "Hello from Tom");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBodyShouldBe("Hello from Laura");
        await WhenIGetUrlOnTheApiGateway("/"); // repeat same request because min MinimumThroughput is 2
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBodyShouldBe("Hello from Laura");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        await WhenIGetUrlOnTheApiGateway("/working");
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBodyShouldBe("Hello from Tom");
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        GivenIWaitMilliseconds(3000);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBodyShouldBe("Hello from Laura");
    }

    // TODO: If failed in parallel execution mode, switch to SequentialTests
    // This issue may arise when transitioning all tests to parallel execution
    // This test must be sequential because of usage of the static DownstreamRoute.DefaultTimeoutSeconds
    [Fact]
    [Trait("Bug", "1833")] // https://github.com/ThreeMammals/Ocelot/issues/1833
    public async Task Should_timeout_per_default_after_90_seconds()
    {
        try
        {
            DownstreamRoute.DefaultTimeoutSeconds = 3; // override original value
            var defTimeoutMs = Ms(DownstreamRoute.DefaultTimeoutSeconds);
            var port = PortFinder.GetRandomPort();
            var route = GivenRoute(port, new QoSOptions(new FileQoSOptions()), HttpMethods.Get);
            var configuration = GivenConfiguration(route);
            GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, defTimeoutMs + 500); // 3.5s > 3s -> ServiceUnavailable
            GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunningWithPolly();
            await WhenIGetUrlOnTheApiGateway("/");
            ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // after 3 secs -> Timeout exception aka request cancellation
        }
        finally
        {
            DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.DefTimeout;
        }
    }

    [Fact]
    [Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public async Task HasRouteAndGlobalTimeouts_RouteTimeoutShouldTakePrecedenceOverGlobalTimeout()
    {
        const int RouteTimeoutSeconds = 2, GlobalTimeoutSeconds = 4;
        int serviceTimeoutMs = Ms(Math.Max(RouteTimeoutSeconds, GlobalTimeoutSeconds)) + 500; // total 4.5 sec

        var port = PortFinder.GetRandomPort();
        var qos = new FileQoSOptions() { TimeoutValue = Ms(RouteTimeoutSeconds) };
        var route = GivenRoute(port, new(qos), HttpMethods.Get);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.QoSOptions.TimeoutValue = Ms(GlobalTimeoutSeconds); // !!!

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, serviceTimeoutMs);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();

        var watcher = await WatchWhenIGetUrlOnTheApiGateway();

        ThenTimeoutIsInRange(watcher, Ms(RouteTimeoutSeconds), Ms(RouteTimeoutSeconds) + 500); // (2.0, 2.5) s
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        await ThenTheResponseBodyShouldBeAsync(string.Empty);
    }

    [Fact]
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public async Task HasGlobalTimeoutOnly_ForAllRoutesGlobalTimeoutShouldTakePrecedenceOverAbsoluteGlobalTimeout()
    {
        const int GlobalTimeoutSeconds = 2;
        int serviceTimeoutMs = Ms(GlobalTimeoutSeconds + 1); // total 3 sec
        var ports = PortFinder.GetPorts(2);
        FileRoute route1 = GivenRoute(ports[0], "/route1"), route2 = GivenRoute(ports[1], "/route2"); // without QoS timeouts
        var configuration = GivenConfiguration(route1, route2);
        configuration.GlobalConfiguration.QoSOptions.TimeoutValue = Ms(GlobalTimeoutSeconds); // !!!
        GivenThereIsAServiceRunningOn(ports[0], HttpStatusCode.OK, serviceTimeoutMs); // 2s -> ServiceUnavailable
        GivenThereIsAServiceRunningOn(ports[1], HttpStatusCode.OK, serviceTimeoutMs); // 2s -> ServiceUnavailable
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();

        var watchers = await Task.WhenAll<Stopwatch>(
            WatchWhenIGetUrlOnTheApiGateway(route1.UpstreamPathTemplate),
            WatchWhenIGetUrlOnTheApiGateway(route2.UpstreamPathTemplate));

        int globalTimeoutMs = Ms(GlobalTimeoutSeconds);
        foreach (var watcher in watchers)
        {
            ThenTimeoutIsInRange(watcher, globalTimeoutMs, Ms(DownstreamRoute.DefaultTimeoutSeconds)); // (2.0, 90) so assert roughly
            ThenTimeoutIsInRange(watcher, globalTimeoutMs, globalTimeoutMs + 500); // (2.0, 2.5) so assert precisely
            ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
            await ThenTheResponseBodyShouldBeAsync(string.Empty);
        }
    }

    [Fact]
    [Trait("PR", "2081")] // https://github.com/ThreeMammals/Ocelot/pull/2081
    [Trait("Feat", "2080")] // https://github.com/ThreeMammals/Ocelot/issues/2080
    public async Task HasRouteAndGlobalFailureRatios_RouteFailureRatioShouldTakePrecedenceOverGlobalFailureRatio()
    {
        const double RouteFailureRatio = 0.50D, GlobalFailureRatio = 0.75D;
        var qos = new FileQoSOptions()
        {
            ExceptionsAllowedBeforeBreaking = 3, // after 3 actions FailureRatio is activated
            DurationOfBreak = CircuitBreakerStrategy.LowBreakDuration + 1,
            FailureRatio = RouteFailureRatio, // 50% of requests
            SamplingDuration = 1_000,
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new(qos), HttpMethods.Get);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.QoSOptions.FailureRatio = GlobalFailureRatio; // !!!

        int count = 0;
        bool isOK = false;
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, 10, () => !isOK && ++count % 2 == 0); // 1 of 2 fails
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK); // 0 failed of 1 -> 0%
        await WhenIGetUrlOnTheApiGateway("/"); // fail
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 1 failed of 2 -> 50% but failure ratio is ignored because of 2 actions < 3
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK); // 1 failed of 3 -> 33%
        await WhenIGetUrlOnTheApiGateway("/"); // fail
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 2 failed of 4 -> 50% -> circuit is open now!
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // 2 failed of 5 -> 40%, but circuit is already open
        await WhenIGetUrlOnTheApiGateway("/"); // fail
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // 3 failed of 6 -> 50%, but circuit is already open
        count.ShouldBe(4); // 2 of 4 were failed, and the service was called 4 times
        isOK = true; // the next requests should be OK
        await Task.Delay(qos.DurationOfBreak.Value); // breaking period is over, thus, circuit breaker is closed
        await WhenIGetUrlOnTheApiGateway("/"); // OK but circuit is closed
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK); // circuit is closed
        await ThenTheResponseBodyShouldBeAsync(nameof(HasRouteAndGlobalFailureRatios_RouteFailureRatioShouldTakePrecedenceOverGlobalFailureRatio));
    }

    [Fact]
    [Trait("PR", "2081")] // https://github.com/ThreeMammals/Ocelot/pull/2081
    [Trait("Feat", "2080")] // https://github.com/ThreeMammals/Ocelot/issues/2080
    public async Task HasGlobalFailureRatioOnly_GlobalFailureRatioShouldTakePrecedenceOverPollyDefaultFailureRatio()
    {
        const double GlobalFailureRatio = 0.75D; // Polly def FailureRatio is CircuitBreakerStrategy.DefaultFailureRatio -> 0.1 -> 10%
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port); // without failure ratios
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.QoSOptions = new()
        {
            ExceptionsAllowedBeforeBreaking = 2, // after 2 actions FailureRatio is activated
            DurationOfBreak = CircuitBreakerStrategy.LowBreakDuration + 1,
            FailureRatio = GlobalFailureRatio, // 75% of requests
            SamplingDuration = 1_000,
        }; // !!!
        int count = 0;
        bool isOK = false;
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, 10, () => !isOK && ++count > 2);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithPolly();

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK); // 0 failed of 1 -> 0%
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK); // 0 failed of 2 -> 0%
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 1 failed of 3 -> 33%
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 2 failed of 4 -> 50%
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 3 failed of 5 -> 60%
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 4 failed of 6 -> 66%
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 5 failed of 7 -> 71%
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError); // 6 failed of 8 -> 75% -> circuit is open now!
        await WhenIGetUrlOnTheApiGateway("/");

        // Assert circuit is open
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // 7 failed of 9 -> 77%, but circuit is already open
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // 8 failed of 10 -> 80%, but circuit is already open
        count.ShouldBe(8); // the service was called 8 times of 10 total
        isOK = true; // the next requests should be OK
        await Task.Delay(configuration.GlobalConfiguration.QoSOptions.DurationOfBreak.Value); // breaking period is over, thus, circuit breaker is closed
        await WhenIGetUrlOnTheApiGateway("/"); // OK but circuit is closed
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK); // circuit is closed
        await ThenTheResponseBodyShouldBeAsync(nameof(HasGlobalFailureRatioOnly_GlobalFailureRatioShouldTakePrecedenceOverPollyDefaultFailureRatio));
    }

    private void GivenOcelotIsRunningWithPolly() => GivenOcelotIsRunning(WithPolly);
    private static void WithPolly(IServiceCollection services) => services.AddOcelot().AddPolly();

    private static void GivenIWaitMilliseconds(int ms) => GivenIWait(ms);

    private HttpStatusCode _brokenServiceStatusCode;
    private void GivenThereIsABrokenServiceRunningOn(int port, HttpStatusCode brokenStatusCode)
    {
        _brokenServiceStatusCode = brokenStatusCode;
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            context.Response.StatusCode = (int)_brokenServiceStatusCode;
            return context.Response.WriteAsync(_brokenServiceStatusCode.ToString());
        });
    }

    private void GivenThereIsABrokenServiceOnline(HttpStatusCode onlineStatusCode)
    {
        _brokenServiceStatusCode = onlineStatusCode;
    }

    private void GivenThereIsAPossiblyBrokenServiceRunningOn(int port, string responseBody)
    {
        int requestCount = 0;
        handler.GivenThereIsAServiceRunningOn(port, async context =>
        {
            if (requestCount == 2)
            {
                // In Polly v8:
                //   MinimumThroughput (exceptions) must be 2 or more
                //   BreakDuration (ex. DurationOfBreak) must be > 500
                //   Timeout (ex. TimeoutValue) must be 1000 or more
                // So, we wait for 2.1 seconds to make sure the circuit is open
                // BreakDuration * MinimumThroughput + Timeout
                // 500 * 2 + 1000 = 2000 minimum + 100 milliseconds to exceed the minimum
                await Task.Delay(2_100);
            }

            requestCount++;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(responseBody);
        });
    }

    private string ResponseBody([CallerMemberName] string response = nameof(PollyQoSTests)) => response;

    protected override void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, int timeout, [CallerMemberName] string response = nameof(PollyQoSTests))
        => base.GivenThereIsAServiceRunningOn(port, statusCode, timeout, response);

    private void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, int timeout, Func<bool> failingStrategy, [CallerMemberName] string response = nameof(PollyQoSTests))
    {
        async Task MapBodyWithTimeout(HttpContext context)
        {
            await Task.Delay(timeout);
            HttpStatusCode status = failingStrategy() ? HttpStatusCode.InternalServerError : statusCode;
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(response);
        }
        handler.GivenThereIsAServiceRunningOn(port, MapBodyWithTimeout);
    }
}
