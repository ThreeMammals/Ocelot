using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Configuration;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Polly;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ocelot.AcceptanceTests;

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
    public void Should_not_timeout()
    {
        var qos = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(10)
            .WithDurationOfBreak(500)
            .WithTimeoutValue(1000)
            .WithFailureRatio(0.5)
            .WithSamplingDuration(5)
            .Build();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos, HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var body = ResponseBody();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, 10, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_timeout()
    {
        var qos = new QoSOptionsBuilder()
            .WithTimeoutValue(1000)
            .Build();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos, HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var body = ResponseBody();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, 2100, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .BDDfy();
    }

    [Fact]
    public void Should_open_circuit_breaker_after_two_exceptions()
    {
        var qos = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithDurationOfBreak(1000)
            .WithTimeoutValue(100_000)
            .Build();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // Polly status
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2085")]
    public void Should_open_circuit_breaker_for_DefaultBreakDuration()
    {
        int invalidDuration = CircuitBreakerStrategy.LowBreakDuration; // valid value must be >500ms, exact 500ms is invalid
        var qos = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithDurationOfBreak(invalidDuration)
            .WithTimeoutValue(100_000)
            .Build();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // Polly status
            .Given(x => GivenIWaitMilliseconds(CircuitBreakerStrategy.DefaultBreakDuration - 500)) // BreakDuration is not elapsed
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // still opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // still opened
            .Given(x => GivenThereIsABrokenServiceOnline(HttpStatusCode.NotFound))
            .Given(x => GivenIWaitMilliseconds(500)) // BreakDuration should elapse now
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // closed, service online
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound)) // closed, service online
            .And(x => ThenTheResponseBodyShouldBe(nameof(HttpStatusCode.NotFound)))
            .BDDfy();
    }

    private const string SkippingOnMacOS = "Skipping the test on MacOS platform: the test is stable in Linux and Windows only!";

    [SkippableFact]
    public void Should_open_circuit_breaker_then_close()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), SkippingOnMacOS);
        var qos = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithDurationOfBreak(CircuitBreakerStrategy.LowBreakDuration + 1) // 501
            .WithTimeoutValue(1000)
            .Build();
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn(port, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min ExceptionsAllowedBeforeBreaking is 2
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .And(x => GivenIWaitMilliseconds(3000))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [SkippableFact] // [Fact]
    public void Open_circuit_should_not_effect_different_route()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), SkippingOnMacOS);
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var qos1 = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithDurationOfBreak(CircuitBreakerStrategy.LowBreakDuration + 1) // 501
            .WithTimeoutValue(1000)
            .Build();
        var route = GivenRoute(port1, qos1);
        var route2 = GivenRoute(port2, new QoSOptionsBuilder().Build(), null, "/working");
        var configuration = GivenConfiguration(route, route2);
        this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn(port1, "Hello from Laura"))
            .And(x => x.GivenThereIsAServiceRunningOn(port2, HttpStatusCode.OK, 0, "Hello from Tom"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min ExceptionsAllowedBeforeBreaking is 2
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .And(x => WhenIGetUrlOnTheApiGateway("/working"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Tom"))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .And(x => GivenIWaitMilliseconds(3000))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    // TODO: If failed in parallel execution mode, switch to SequentialTests
    // This issue may arise when transitioning all tests to parallel execution
    // This test must be sequential because of usage of the static DownstreamRoute.DefaultTimeoutSeconds
    [Fact]
    [Trait("Bug", "1833")]
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

    private static void GivenIWaitMilliseconds(int ms) => Thread.Sleep(ms);

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
                //   MinimumThroughput (ExceptionsAllowedBeforeBreaking) must be 2 or more
                //   BreakDuration (DurationOfBreak) must be > 500
                //   Timeout (TimeoutValue) must be 1000 or more
                // So, we wait for 2.1 seconds to make sure the circuit is open
                // DurationOfBreak * ExceptionsAllowedBeforeBreaking + Timeout
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
