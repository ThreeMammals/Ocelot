using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Configuration;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Polly;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ocelot.AcceptanceTests;

public sealed class PollyQoSTests : TimeoutTestsBase
{
    private static FileRoute GivenRoute(int port, QoSOptions options, string httpMethod = null, string upstream = null)
    {
        var route = GivenRoute(port, upstream, null);
        route.UpstreamHttpMethod = [ httpMethod ?? HttpMethods.Get ];
        route.QoSOptions = new(options);
        return route;
    }

    [Fact]
    public void Should_not_timeout()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(10, 500, .5, 5, 1000, null), HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, string.Empty, 10))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_timeout()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(0, 0, 1000, null), HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty, 2100))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .BDDfy();
    }

    [Fact]
    public void Should_open_circuit_breaker_after_two_exceptions()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(2, 1000, 100000, null));
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
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(2, invalidDuration, .005,1,100000, null));
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

    [SkippableFact] // [Fact]
    public void Should_open_circuit_breaker_then_close()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), SkippingOnMacOS);

        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(2, 500, 1000, null));
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn(port, "Hello from Laura"))
            .Given(x => GivenThereIsAConfiguration(configuration))
            .Given(x => GivenOcelotIsRunningWithPolly())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min ExceptionsAllowedBeforeBreaking is 2
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .Given(x => WhenIGetUrlOnTheApiGateway("/"))
            .Given(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .Given(x => WhenIGetUrlOnTheApiGateway("/"))
            .Given(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .Given(x => WhenIGetUrlOnTheApiGateway("/"))
            .Given(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .Given(x => GivenIWaitMilliseconds(3000))
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
        var qos1 = new QoSOptions(2, 500, 1000, null);
        var route = GivenRoute(port1, qos1);
        var route2 = GivenRoute(port2, new(new FileQoSOptions()), null, "/working");
        var configuration = GivenConfiguration(route, route2);
        this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn(port1, "Hello from Laura"))
            .And(x => x.GivenThereIsAServiceRunningOn(port2, HttpStatusCode.OK, "Hello from Tom", 0))
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
            GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty, defTimeoutMs + 500); // 3.5s > 3s -> ServiceUnavailable
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

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty, serviceTimeoutMs);
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
        var requestCount = 0;
        handler.GivenThereIsAServiceRunningOn(port, async context =>
        {
            if (requestCount == 2)
            {
                // In Polly v8:
                //   MinimumThroughput (ExceptionsAllowedBeforeBreaking) must be 2 or more
                //   BreakDuration (DurationOfBreak) must be 500 or more
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

    private void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, string responseBody, int timeout)
    {
        handler.GivenThereIsAServiceRunningOn(port, async context =>
        {
            await Task.Delay(timeout);
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(responseBody);
        });
    }
}
