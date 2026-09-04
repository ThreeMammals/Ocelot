using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.QualityOfService;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ocelot.Acceptance.QualityOfService;

[Trait("Feat", "23")] // https://github.com/ThreeMammals/Ocelot/issues/23
[Trait("Feat", "39")] // https://github.com/ThreeMammals/Ocelot/pull/39
public sealed class QualityOfServiceTests : QosSteps
{
    public const bool NoDiscovery = false;

    [Fact]
    [Trait("Feat", "318")] // https://github.com/ThreeMammals/Ocelot/issues/318
    [Trait("PR", "319")] // https://github.com/ThreeMammals/Ocelot/pull/319
    public void Should_not_timeout()
    {
        const int timeout = 10;
        var qos = new QoSOptions()
        {
            BreakDuration = 500,
            MinimumThroughput = 10,
            FailureRatio = 0.5,
            SamplingDuration = 5,
            Timeout = 1000, // !!!
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, timeout, body)) // !!!
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "318")] // https://github.com/ThreeMammals/Ocelot/issues/318
    [Trait("PR", "319")] // https://github.com/ThreeMammals/Ocelot/pull/319
    public void Should_timeout()
    {
        const int ServiceTimeout = 2100;
        var qos = new QoSOptions(1000); // timeout
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, ServiceTimeout, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
        .BDDfy();
    }

    [Fact]
    [Trait("Bug", "1550")] // https://github.com/ThreeMammals/Ocelot/issues/1550
    [Trait("Bug", "1706")] // https://github.com/ThreeMammals/Ocelot/issues/1706
    [Trait("PR", "1753")] // https://github.com/ThreeMammals/Ocelot/pull/1753
    public void Should_open_circuit_breaker_after_two_exceptions()
    {
        var qos = new QoSOptions(2, 1000)
        {
            Timeout = 100_000, // infinite -> actually no timeout
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError, 0))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenIGetUrlOnTheApiGatewayTimesThenIExpectStatus(qos.MinimumThroughput.Value, HttpStatusCode.InternalServerError))
            .And(x => WhenIGetUrlOnTheApiGateway("/")) // opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // Polly status
        .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2085")] // https://github.com/ThreeMammals/Ocelot/issues/2085
    public void Should_open_circuit_breaker_for_DefaultBreakDuration()
    {
        int cicdMs = IsCiCd() ? 50 : 0;
        int invalidDuration = CircuitBreakerDelegatingHandler.LowBreakDuration; // valid value must be >500ms, exact 500ms is invalid
        var qos = new QoSOptions(2, invalidDuration)
        {
            Timeout = 100_000,
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError, 0))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // Polly status
            .Given(x => GivenIWaitMilliseconds(CircuitBreakerDelegatingHandler.DefaultBreakDuration - 500)) // 5000 - 500 = 4500; BreakDuration is not elapsed
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // still opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // still opened
            .Given(x => GivenThereIsABrokenServiceOnline(HttpStatusCode.NotFound, 0, 1, NoDiscovery))
            .And(x => GivenIWaitMilliseconds(500 + cicdMs)) // BreakDuration should elapse now
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // closed, service online
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound)) // closed, service online
            .And(x => ThenTheResponseBodyShouldBe(nameof(HttpStatusCode.NotFound)))
        .BDDfy();
    }

    /// <summary>
    /// Verifies that when upstream Responses exceed the configured timeout, those failures contribute to opening the circuit breaker,
    /// and that after the break period elapses the circuit closes again so requests can succeed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous acceptance test.</returns>
    [Fact]
    [Trait("PR", "39")] // https://github.com/ThreeMammals/Ocelot/pull/39
    public void Should_open_circuit_breaker_then_close()
    {
        const int MillisecondsDelay = 2_100;
        var qos = new QoSOptions(CircuitBreakerDelegatingHandler.LowMinimumThroughput, CircuitBreakerDelegatingHandler.LowBreakDuration + 1) // 501
        {
            Timeout = 1000, // -> TimeoutRejectedException
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .And(x => GivenThereIsAPossiblyBrokenServiceRunningOn(port, "Hello from Laura", MillisecondsDelay, 2))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheResponseShouldBeAsync(HttpStatusCode.OK, "Hello from Laura"))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min MinimumThroughput is 2
            .Then(x => ThenTheResponseShouldBeAsync(HttpStatusCode.OK, "Hello from Laura"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .Given(x => GivenIWaitMilliseconds(MillisecondsDelay))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .And(x => ThenTheResponseShouldBeAsync(HttpStatusCode.OK, "Hello from Laura"))
        .BDDfy();
    }

    [Fact] // [SkippableFact]
    [Trait("PR", "39")] // https://github.com/ThreeMammals/Ocelot/pull/39
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    public void Should_open_circuit_breaker_then_close_without_timeout_strategy()
    {
        //Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), SkippingOnMacOS);
        var qos = new QoSOptions(CircuitBreakerDelegatingHandler.LowMinimumThroughput, 1000) // 501
        {
            Timeout = null, // switch off timeout strategy
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .Then(x => TestRouteCircuitBreaker(new int[] { port }, route.UpstreamPathTemplate, route.QoSOptions, 0, NoDiscovery))
        .BDDfy();
    }

    [Fact] // [SkippableFact]
    [Trait("PR", "39")] // https://github.com/ThreeMammals/Ocelot/pull/39
    public void Open_circuit_should_not_effect_different_route()
    {
        // Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), SkippingOnMacOS);
        const int MillisecondsDelay = 2_100;
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var qos1 = new QoSOptions(2, CircuitBreakerDelegatingHandler.LowBreakDuration + 1) // 501
        {
            Timeout = 1000,
        };
        var route = GivenRoute(port1, qos1);
        var route2 = GivenRoute(port2, new(), "/working");
        var configuration = GivenConfiguration(route, route2);
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .And(x => GivenThereIsAPossiblyBrokenServiceRunningOn(port1, "Hello from Laura", MillisecondsDelay, 2))
            .And(x => GivenThereIsAServiceRunningOn(port2, HttpStatusCode.OK, 0, "Hello from Tom"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min MinimumThroughput is 2
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .When(x => WhenIGetUrlOnTheApiGateway("/working"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyShouldBe("Hello from Tom"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .Given(x => GivenIWaitMilliseconds(3000))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }

    // TODO: If failed in parallel execution mode, switch to SequentialTests
    // This issue may arise when transitioning all tests to parallel execution
    // This test must be sequential because of usage of the static DownstreamRoute.DefaultTimeoutSeconds
    [Fact]
    [Trait("Bug", "1833")] // https://github.com/ThreeMammals/Ocelot/issues/1833
    public void Should_timeout_per_default_after_90_seconds()
    {
        var body = Body();
        try
        {
            DownstreamRoute.DefaultTimeoutSeconds = 3; // override original value
            var defTimeoutMs = Ms(DownstreamRoute.DefaultTimeoutSeconds);
            var port = PortFinder.GetRandomPort();
            var route = GivenRoute(port, new(new FileQoSOptions()));
            var configuration = GivenConfiguration(route);
            this
                .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, defTimeoutMs + 500, body)) // 3.5s > 3s -> GatewayTimeout
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.GatewayTimeout)) // after 3 secs -> TimeoutException by TimeoutDelegatingHandler
            .BDDfy();
        }
        finally
        {
            DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.DefTimeout;
        }
    }

    [Fact]
    [Trait("PR", "2073")] // https://github.com/ThreeMammals/Ocelot/pull/2073
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public void HasRouteAndGlobalTimeouts_RouteTimeoutShouldTakePrecedenceOverGlobalTimeout()
    {
        const int RouteTimeoutSeconds = 2, GlobalTimeoutSeconds = 4;
        int serviceTimeoutMs = Ms(Math.Max(RouteTimeoutSeconds, GlobalTimeoutSeconds)) + 500; // total 4.5 sec

        var port = PortFinder.GetRandomPort();
        var qos = new FileQoSOptions() { Timeout = Ms(RouteTimeoutSeconds) };
        var route = GivenRoute(port, new(qos));
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.QoSOptions = new() { Timeout = Ms(GlobalTimeoutSeconds) }; // !!!
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, serviceTimeoutMs, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenImWatchingWhenIGetUrlOnTheApiGateway())
            .Then(x => ThenTimeoutIsInRange(_watcher, Ms(RouteTimeoutSeconds), Ms(RouteTimeoutSeconds) + 500)) // (2.0, 2.5) s
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .And(x => response.ReasonPhrase.ShouldBe("Request timeout", "Request timeout"))
            .And(x => ThenTheResponseBodyShouldBe("Request timeout for route -> /"))
        .BDDfy();
    }
    private Stopwatch _watcher;
    private async Task WhenImWatchingWhenIGetUrlOnTheApiGateway()
        => _watcher = await WatchWhenIGetUrlOnTheApiGateway();

    [Fact]
    [Trait("Feat", "1314")] // https://github.com/ThreeMammals/Ocelot/issues/1314
    public void HasGlobalTimeoutOnlyThenForAllRoutesGlobalTimeoutShouldTakePrecedenceOverAbsoluteGlobalTimeout()
    {
        const int GlobalTimeoutSeconds = 2;
        int serviceTimeoutMs = Ms(GlobalTimeoutSeconds + 1); // total 3 sec
        var ports = PortFinder.GetPorts(2);
        FileRoute route1 = GivenRoute(ports[0], "/route1"),
            route2 = GivenRoute(ports[1], "/route2"); // without QoS timeouts
        var configuration = GivenConfiguration(route1, route2);
        configuration.GlobalConfiguration.QoSOptions = new() { Timeout = Ms(GlobalTimeoutSeconds) }; // !!!
        var body = Body();
        int globalTimeoutMs = Ms(GlobalTimeoutSeconds);
        var responses = new HttpResponseMessage[2];
        this
            .Given(x => GivenThereIsAServiceRunningOn(ports[0], HttpStatusCode.OK, serviceTimeoutMs, body)) // 2s -> ServiceUnavailable
            .Given(x => GivenThereIsAServiceRunningOn(ports[1], HttpStatusCode.OK, serviceTimeoutMs, body)) // 2s -> ServiceUnavailable
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenImWatchingWhenIGetUrlOnTheApiGateway(
                WatchWhenIGetUrlOnTheApiGateway(route1.UpstreamPathTemplate),
                WatchWhenIGetUrlOnTheApiGateway(route2.UpstreamPathTemplate)))
            .Then(x => ThenTimeoutIsInRange(_watchers[0], globalTimeoutMs, Ms(DownstreamRoute.DefaultTimeoutSeconds))) // (2.0, 90) so assert roughly
            .And(x => ThenTimeoutIsInRange(_watchers[0], globalTimeoutMs, globalTimeoutMs + 500)) // (2.0, 2.5) so assert precisely
            .Then(x => ThenTimeoutIsInRange(_watchers[1], globalTimeoutMs, Ms(DownstreamRoute.DefaultTimeoutSeconds))) // (2.0, 90) so assert roughly
            .And(x => ThenTimeoutIsInRange(_watchers[1], globalTimeoutMs, globalTimeoutMs + 500)) // (2.0, 2.5) so assert precisely
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // after 2 secs -> TimeoutException by TimeoutDelegatingHandler
            .And(x => response.ReasonPhrase.ShouldBe("Request timeout", "Request timeout"))
            // ThenTheResponseBodyShouldBeAsync("Request timeout for route -> /route2");
            .And(x => ResponseBodyShouldStartWith("Request timeout for route -> /route")) // route1 or route2 due to load balancing
        .BDDfy();
    }
    private Stopwatch[] _watchers;
    private async Task WhenImWatchingWhenIGetUrlOnTheApiGateway(params Task<Stopwatch>[] watchees)
        => _watchers = await Task.WhenAll(watchees);
    private async Task ResponseBodyShouldStartWith(string expected)
        => (await response.Content.ReadAsStringAsync(CancelMe)).ShouldStartWith(expected);

    [Fact]
    [Trait("PR", "2081")] // https://github.com/ThreeMammals/Ocelot/pull/2081
    [Trait("Feat", "2080")] // https://github.com/ThreeMammals/Ocelot/issues/2080
    public void HasRouteAndGlobalFailureRatiosThenRouteFailureRatioShouldTakePrecedenceOverGlobalFailureRatio()
    {
        const double RouteFailureRatio = 0.50D, GlobalFailureRatio = 0.75D;
        var qos = new FileQoSOptions()
        {
            MinimumThroughput = 3, // after 3 actions FailureRatio is activated
            BreakDuration = CircuitBreakerDelegatingHandler.LowBreakDuration + 1,
            FailureRatio = RouteFailureRatio, // 50% of requests
            SamplingDuration = 1_000,
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new(qos));
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.QoSOptions = new() { FailureRatio = GlobalFailureRatio }; // !!!

        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, TimeoutStrategy, FailEvery2ndReqStrategy, body)) // 1 of 2 fails
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK)) // 0 failed of 1 -> 0%
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // fail
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 1 failed of 2 -> 50% but failure ratio is ignored because of 2 actions < 3
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK)) // 1 failed of 3 -> 33%
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // fail
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 2 failed of 4 -> 50% -> circuit is open now!
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // 2 failed of 5 -> 40%, but circuit is already open
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // fail
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // 3 failed of 6 -> 50%, but circuit is already open
            .And(x => ThenCountShouldBe(4)) // 2 of 4 were failed, and the service was called 4 times
            .Given(x => GivenTheNextRequestsShouldBeOk(true))
            .And(x => GivenIWaitMilliseconds(qos.BreakDuration.Value + (IsCiCd() ? 50 : 0))) // breaking period is over, thus, circuit breaker is closed
            .And(x => WhenIGetUrlOnTheApiGateway("/")) // OK but circuit is closed
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK)) // circuit is closed
            .And(x => ThenTheResponseBodyShouldBeAsync(body))
        .BDDfy();
    }
    private int _count = 0;
    private bool _isOK = false;
    private void ThenCountShouldBe(int count) => _count.ShouldBe(count);
    private void GivenTheNextRequestsShouldBeOk(bool isOK) => _isOK = isOK;
    private static int TimeoutStrategy() => 10;
    private bool FailEvery2ndReqStrategy() => !_isOK && ++_count % 2 == 0;

    [Fact]
    [Trait("PR", "2081")] // https://github.com/ThreeMammals/Ocelot/pull/2081
    [Trait("Feat", "2080")] // https://github.com/ThreeMammals/Ocelot/issues/2080
    public void HasGlobalFailureRatioOnlyThenGlobalFailureRatioShouldTakePrecedenceOverPollyDefaultFailureRatio()
    {
        const double GlobalFailureRatio = 0.75D; // Polly def FailureRatio is CircuitBreakerStrategy.DefaultFailureRatio -> 0.1 -> 10%
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port); // without failure ratios
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.QoSOptions = new()
        {
            MinimumThroughput = 2, // after 2 actions FailureRatio is activated
            BreakDuration = CircuitBreakerDelegatingHandler.LowBreakDuration + 1,
            FailureRatio = GlobalFailureRatio, // 75% of requests
            SamplingDuration = 1_000,
        }; // !!!
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, TimeoutStrategy, FailAfter2ndReqStrategy, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK)) // 0 failed of 1 -> 0%

            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK)) // 0 failed of 2 -> 0%
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 1 failed of 3 -> 33%
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 2 failed of 4 -> 50%
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 3 failed of 5 -> 60%
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 4 failed of 6 -> 66%
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 5 failed of 7 -> 71%
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // 6 failed of 8 -> 75% -> circuit is open now!
            .When(x => WhenIGetUrlOnTheApiGateway("/"))

            // Assert circuit is open
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // 7 failed of 9 -> 77%, but circuit is already open
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // 8 failed of 10 -> 80%, but circuit is already open
            .And(x => ThenCountShouldBe(8)) // the service was called 8 times of 10 total
            .Given(x => GivenTheNextRequestsShouldBeOk(true)) // the next requests should be OK
            .And(x => GivenIWaitMilliseconds(configuration.GlobalConfiguration.QoSOptions.BreakDuration.Value + (IsCiCd() ? 50 : 0))) // breaking period is over, thus, circuit breaker is closed
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // OK but circuit is closed
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK)) // circuit is closed
            .And(x => ThenTheResponseBodyShouldBeAsync(body))
        .BDDfy();
    }
    private bool FailAfter2ndReqStrategy() => !_isOK && ++_count > 2;

    /// <summary>
    /// Validates that <see cref="QoSOptions.MinimumThroughput"/> acts as a gate:
    /// even with a 100% failure rate, the circuit stays <b>closed</b> while the total number
    /// of calls within the <see cref="QoSOptions.SamplingDuration"/> window is below <see cref="QoSOptions.MinimumThroughput"/>.
    /// </summary>
    /// <remarks>
    /// Scenario from <see href="https://github.com/ocelotgateway/Ocelot/issues/6#issuecomment-4304670722">issue #6</see>:
    /// <c>FailureRatio = 0.5</c>, <c>MinimumThroughput = 8</c>, <c>SamplingDuration = 10 s</c> —
    /// 7 calls, all failing → circuit <b>stays closed</b> (throughput too low).
    /// </remarks>
    [Fact]
    [Trait("Feat", "2384")] // https://github.com/ThreeMammals/Ocelot/issues/2384
    [Trait("PR", "2385")] // https://github.com/ThreeMammals/Ocelot/pull/2385
    public void FailureRatioWhenBelowMinimumThroughputThenCircuitStaysClosed()
    {
        const int minimumThroughput = 8;
        var qos = new QoSOptions(minimumThroughput, CircuitBreakerDelegatingHandler.LowBreakDuration + 1)
        {
            FailureRatio = 0.5, // 50 %
            SamplingDuration = 30_000, // 30 s – long enough that no entry expires during the test
            Timeout = 100_000,
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError, 0))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))

            // Send (MinimumThroughput – 1) = 7 all-failing requests.
            // The failure ratio (100 %) already exceeds FailureRatio (50 %), but
            // MinimumThroughput is NOT yet reached → the ratio check is skipped entirely.
            // Every response must come directly from the downstream service (500), NOT from
            // the circuit breaker (503).
            .When(x => WhenIGetUrlOnTheApiGatewayTimesThenIExpectStatus(minimumThroughput - 1, HttpStatusCode.InternalServerError))
            .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .And(x => ThenTheResponseBodyShouldBe(nameof(HttpStatusCode.InternalServerError)))
        .BDDfy();
    }

    /// <summary>
    /// Validates that the circuit <b>opens</b> once both conditions are simultaneously met:
    /// the total calls in the sampling window reach <see cref="QoSOptions.MinimumThroughput"/>
    /// <b>and</b> the observed failure ratio exceeds <see cref="QoSOptions.FailureRatio"/>.
    /// </summary>
    /// <remarks>
    /// Scenario from <see href="https://github.com/ocelotgateway/Ocelot/issues/6#issuecomment-4304670722">issue #6</see>:
    /// <c>FailureRatio = 0.5</c>, <c>MinimumThroughput = 8</c>, <c>SamplingDuration = 10 s</c> —
    /// 8 calls, 5 failing (62.5 %) → circuit <b>opens</b>.
    /// </remarks>
    [Fact]
    [Trait("Feat", "2384")] // https://github.com/ThreeMammals/Ocelot/issues/2384
    [Trait("PR", "2385")] // https://github.com/ThreeMammals/Ocelot/pull/2385
    public void FailureRatioWhenAtMinimumThroughputWithExceededRatioThenCircuitOpens()
    {
        // 3 successes + 5 failures = 5/8 = 62.5 % ≥ 50 % (FailureRatio) AND 8 ≥ 8 (MinimumThroughput) → opens
        const int minimumThroughput = 8;
        const int successCalls = 3;   // calls 1-3 succeed
        const int failureCalls = 5;   // calls 4-8 fail → 5/8 = 62.5 %
        var qos = new QoSOptions(minimumThroughput, CircuitBreakerDelegatingHandler.LowBreakDuration + 1)
        {
            FailureRatio = 0.5, // 50 %
            SamplingDuration = 30_000, // 30 s – long enough that no entry expires during the test
            Timeout = 100_000,
        };
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        var body = Body();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, TimeoutStrategy, () => FailFirstNthReqStrategy(successCalls), body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))

            // Calls 1-3: all succeed (0 failures / 3 total = 0 %, below MinimumThroughput)
            .When(x => WhenIGetUrlOnTheApiGatewayTimesThenIExpectStatus(successCalls, HttpStatusCode.OK)) // circuit closed, reached service

            // Calls 4-7: four consecutive failures — ratio keeps rising but total < 8 (MinimumThroughput)
            .When(x => WhenIGetUrlOnTheApiGatewayTimesThenIExpectStatus(failureCalls - 1, HttpStatusCode.InternalServerError)) // circuit still closed (total < 8)

            // Call 8: 5th failure — total = 8 ≥ MinimumThroughput AND ratio = 5/8 = 62.5 % ≥ 50 % → circuit OPENS
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError)) // reached service; circuit opens after this call

            // Call 9: circuit is now OPEN → blocked immediately with 503
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
        .BDDfy();
    }
    private int _callCount = 0;
    private bool FailFirstNthReqStrategy(int successCalls) => ++_callCount > successCalls;

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    public void ShouldApplyGlobalQosOptionsForStaticRoutes()
    {
        const int GlobalTimeout = 1500;
        const int RouteExceptions = 2, GlobalExceptions = 3;
        const int RouteBreakMs = 1000, GlobalBreakMs = 2000;
        var ports = PortFinder.GetPorts(3);
        var route1 = GivenRoute(ports[0],
            options: null!, // no opts -> use global opts
            "/route1");
        var route2 = GivenRoute(ports[1],
            new QoSOptions(RouteExceptions, RouteBreakMs),
            "/route2");
        var route3 = GivenRoute(ports[2],
            new QoSOptions(0, 0) { Timeout = GlobalTimeout }, // disable Circuit Breaker via disallowing of global opts to substitute
            "/noCircuitBreaker");
        var configuration = GivenConfiguration(route1, route2, route3); // static routes come to Routes collection
        var globalOptions = configuration.GlobalConfiguration.QoSOptions
            = new(new QoSOptions(GlobalExceptions, GlobalBreakMs));
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))

            // TODO: Add acceptance steps that are more parallelism-friendly.
            // The code below failed due to a shared response object being used for sequential steps.
            //await Task.WhenAll(
            //    TestRouteCircuitBreaker(route1, 0, globalOptions), // test global scenario
            //    TestRouteCircuitBreaker(route2, 1), // test route-level scenario
            //    TestRouteTimeout(route3));
            .When(x => TestRouteCircuitBreaker(ports.Skip(0).Take(1).ToArray(), route1.UpstreamPathTemplate, globalOptions, 0, NoDiscovery)) // test global scenario
            .When(x => TestRouteCircuitBreaker(ports.Skip(1).Take(1).ToArray(), route2.UpstreamPathTemplate, route2.QoSOptions, 1, NoDiscovery)) // test route-level scenario
            .Then(x => TestRouteTimeout(ports.Skip(2).Take(1).ToArray(), route3.UpstreamPathTemplate, route3.QoSOptions))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    public void ShouldApplyGlobalQosOptionsForStaticRoutesWithGroupedOpts()
    {
        const int GlobalTimeout = 1500, GlobalExceptions = 3, GlobalBreakMs = 2000;
        var ports = PortFinder.GetPorts(3);

        // 1st route
        var route1 = GivenRoute(ports[0],
            options: null!, // no opts -> no QoS at all
            "/route1");
        route1.Key = null; // 1st route is not in the global group

        // 2nd route
        var route2 = GivenRoute(ports[1],
            options: null!, // 2nd route opts will be applied from global ones
            "/route2");
        route2.Key = "R2"; // 2nd route is in the group

        // 3rd route
        var route3 = GivenRoute(ports[2],
            new QoSOptions(0, 0) { Timeout = GlobalTimeout }, // disable Circuit Breaker via disallowing of global opts to substitute
            "/noCircuitBreaker");

        var configuration = GivenConfiguration(route1, route2, route3); // static routes come to Routes collection
        var globalOptions = configuration.GlobalConfiguration.QoSOptions
            = new(new QoSOptions(GlobalExceptions, GlobalBreakMs))
            {
                RouteKeys = ["R2"],
            };
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfService))
            .Then(x => TestRouteCircuitBreaker(new int[] { ports[0] }, route1.UpstreamPathTemplate, route1.QoSOptions, 0, NoDiscovery)) // no QoS scenario
            .Given(x => GivenThereIsABrokenServiceOnline(HttpStatusCode.OK, 0, 1, NoDiscovery)) // bring 1st service back online
            .When(x => WhenIGetUrlOnTheApiGateway(route1.UpstreamPathTemplate))
            .Then(x => ThenTheResponseShouldBeAsync(HttpStatusCode.OK, "OK"))
            .When(x => TestRouteCircuitBreaker(new int[] { ports[1] }, route2.UpstreamPathTemplate, globalOptions, 1,  NoDiscovery)) // test global scenario
            .Then(x => TestRouteTimeout(new int[] { route3.DownstreamHostAndPorts[0].Port }, route3.UpstreamPathTemplate, route3.QoSOptions))
        .BDDfy();
    }

    /// <summary>
    /// Verifies that <see cref="IOcelotBuilder.AddQualityOfService{THandler}()"/> registers a custom
    /// <see cref="CircuitBreakerDelegatingHandler"/> subclass end-to-end: the custom handler's overridden
    /// <see cref="CircuitBreakerDelegatingHandler.ServerErrorCodes"/> set (which adds
    /// <see cref="HttpStatusCode.NotFound"/> as a failure code) causes the circuit to open after
    /// <see cref="QoSOptions.MinimumThroughput"/> consecutive 404 Responses — something the default
    /// built-in handler would never do, because 404 is not in <see cref="CircuitBreakerDelegatingHandler.DefaultServerErrorCodes"/>.
    /// </summary>
    [Fact]
    [Trait("Feat", "2384")] // https://github.com/ThreeMammals/Ocelot/issues/2384
    [Trait("PR", "2385")] // https://github.com/ThreeMammals/Ocelot/pull/2385
    public void AddQualityOfServiceWhenGenericCustomServerErrorCodesThenOpensCircuitOn404()
    {
        const int minimumThroughput = 2;
        var qos = new QoSOptions(minimumThroughput, 5000);
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, qos);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.NotFound, 0))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync(WithQualityOfServiceCustomHandler))

            // The first two 404 Responses are recorded as failures by the custom handler.
            .When(x => WhenIGetUrlOnTheApiGatewayTimesThenIExpectStatus(minimumThroughput, HttpStatusCode.NotFound)) // reached service; failure recorded

            // After MinimumThroughput failures the circuit is open: the next request is rejected immediately.
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // circuit open
        .BDDfy();
    }

    private FileRoute GivenRoute(int port, QoSOptions options, string upstream = null, string method = null)
    {
        var route = GivenRoute(port, upstream, upstream);
        route.UpstreamHttpMethod = [method ?? HttpMethods.Get];
        route.QoSOptions = options is null ? null : new(options);
        return route;
    }

    private static void WithQualityOfService(IServiceCollection services)
        => services.AddOcelot().AddQualityOfService();

    private static void WithQualityOfServiceCustomHandler(IServiceCollection services)
        => services.AddOcelot().AddQualityOfService<NotFoundCircuitBreakerHandler>();

    /// <summary>
    /// A <see cref="CircuitBreakerDelegatingHandler"/> subclass that adds <see cref="HttpStatusCode.NotFound"/>
    /// to the set of status codes that are treated as circuit-breaker failures.
    /// </summary>
    private sealed class NotFoundCircuitBreakerHandler : CircuitBreakerDelegatingHandler
    {
        public NotFoundCircuitBreakerHandler(DownstreamRoute route, IOcelotLoggerFactory loggerFactory)
            : base(route, loggerFactory) { }

        protected override HashSet<HttpStatusCode> ServerErrorCodes { get; } =
            new(DefaultServerErrorCodes) { HttpStatusCode.NotFound };
    }

    private Task GivenIWaitMilliseconds(int ms) => GivenIWaitAsync(ms);

    private void GivenThereIsAPossiblyBrokenServiceRunningOn(int port, string responseBody, int millisecondsDelay, int requestNo = 2)
    {
        int requestCount = 0;
        handler.GivenThereIsAServiceRunningOn(port, async context =>
        {
            if (requestCount == requestNo)
            {
                // In Polly v8:
                //   MinimumThroughput (exceptions) must be 2 or more
                //   BreakDuration (ex. DurationOfBreak) must be > 500
                //   Timeout (ex. TimeoutValue) must be 1000 or more
                // So, we wait for 2.1 seconds to make sure the circuit is open
                // BreakDuration * MinimumThroughput + Timeout
                // 500 * 2 + 1000 = 2000 minimum + 100 milliseconds to exceed the minimum
                await Task.Delay(millisecondsDelay); // 2_100
            }

            requestCount++;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(responseBody);
        });
    }

    public override void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, int timeout, [CallerMemberName] string response = nameof(QualityOfServiceTests))
        => base.GivenThereIsAServiceRunningOn(port, statusCode, timeout, response);

    private async Task WhenIGetUrlOnTheApiGatewayTimesThenIExpectStatus(int times, HttpStatusCode expected)
    {
        for (int i = 0; i < times; i++)
        {
            await WhenIGetUrlOnTheApiGateway("/");
            ThenTheStatusCodeShouldBe(expected);
        }
    }
}
