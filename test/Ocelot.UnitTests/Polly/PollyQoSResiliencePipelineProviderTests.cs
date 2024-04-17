using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Testing;
using Polly.Timeout;

namespace Ocelot.UnitTests.Polly;

public class PollyQoSResiliencePipelineProviderTests
{
    [Fact]
    public void Should_build()
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(1000) // 1s, minimum required by Polly
            .WithExceptionsAllowedBeforeBreaking(2) // 2 is the minimum required by Polly
            .WithDurationOfBreak(500) // 0.5s, minimum required by Polly
            .Build();

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();

        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var resiliencePipelineRegistry = new ResiliencePipelineRegistry<OcelotResiliencePipelineKey>();
        var pollyQoSResiliencePipelineProvider = new PollyQoSResiliencePipelineProvider(loggerFactoryMock.Object, resiliencePipelineRegistry);
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);
        resiliencePipeline.ShouldNotBeNull();
    }

    [Fact]
    public void Should_return_same_circuit_breaker_for_given_route()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();
        var route1 = DownstreamRouteFactory("/");
        var route2 = DownstreamRouteFactory("/");

        var resiliencePipeline1 = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route1);
        var resiliencePipeline2 = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route2);
        resiliencePipeline1.ShouldBe(resiliencePipeline2);

        var resiliencePipeline3 = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route1);
        resiliencePipeline3.ShouldBe(resiliencePipeline1);
        resiliencePipeline3.ShouldBe(resiliencePipeline2);
    }

    [Fact]
    public void Should_return_different_circuit_breaker_for_two_different_routes()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();
        var route1 = DownstreamRouteFactory("/");
        var route2 = DownstreamRouteFactory("/test");

        var resiliencePipeline1 = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route1);
        var resiliencePipeline2 = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route2);

        resiliencePipeline1.ShouldNotBe(resiliencePipeline2);
    }

    [Fact]
    public void Should_build_and_wrap_contains_two_policies()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        var route = DownstreamRouteFactory("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);
        resiliencePipeline.ShouldNotBeNull();

        var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();
        resiliencePipelineDescriptor.ShouldNotBeNull();
        resiliencePipelineDescriptor.Strategies.Count.ShouldBe(2);
        resiliencePipelineDescriptor.Strategies[0].Options.ShouldBeOfType<TimeoutStrategyOptions>();
        resiliencePipelineDescriptor.Strategies[1].Options.ShouldBeOfType<CircuitBreakerStrategyOptions<HttpResponseMessage>>();
    }

    [Fact]
    public void Should_build_and_contains_one_policy_when_with_exceptions_allowed_before_breaking_is_zero()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        // get route with 0 exceptions allowed before breaking
        var route = DownstreamRouteFactory("/", true);
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);
        resiliencePipeline.ShouldNotBeNull();

        var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();
        resiliencePipelineDescriptor.ShouldNotBeNull();
        resiliencePipelineDescriptor.Strategies.Count.ShouldBe(1);
        resiliencePipelineDescriptor.Strategies.Single().Options.ShouldBeOfType<TimeoutStrategyOptions>();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported)]
    [InlineData(HttpStatusCode.VariantAlsoNegotiates)]
    [InlineData(HttpStatusCode.InsufficientStorage)]
    [InlineData(HttpStatusCode.LoopDetected)]
    public async Task Should_throw_broken_circuit_exception_after_two_exceptions(HttpStatusCode errorCode)
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        var route = DownstreamRouteFactory("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);

        var response = new HttpResponseMessage(errorCode);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_not_throw_broken_circuit_exception_if_status_code_ok()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        var route = DownstreamRouteFactory("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
    }

    [Fact]
    public async Task Should_throw_and_before_delay_should_not_allow_requests()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        var route = DownstreamRouteFactory("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(200);

        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_internal_server_error()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        var route = DownstreamRouteFactory("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(6000);

        Assert.Equal(HttpStatusCode.InternalServerError, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_internal_server_error_and_throw()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        var route = DownstreamRouteFactory("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(6000);

        Assert.Equal(HttpStatusCode.InternalServerError, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_ok_request_and_put_counter_back_to_zero()
    {
        var pollyQoSResiliencePipelineProvider = PollyQoSResiliencePipelineProviderFactory();

        var route = DownstreamRouteFactory("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(10000);

        var response2 = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response2))).StatusCode);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    private static PollyQoSResiliencePipelineProvider PollyQoSResiliencePipelineProviderFactory()
    {
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        loggerFactoryMock
            .Setup(x => x.CreateLogger<PollyQoSResiliencePipelineProvider>())
            .Returns(new Mock<IOcelotLogger>().Object);
        var resiliencePipelineRegistry = new ResiliencePipelineRegistry<OcelotResiliencePipelineKey>();

        var pollyQoSResiliencePipelineProvider = new PollyQoSResiliencePipelineProvider(loggerFactoryMock.Object, resiliencePipelineRegistry);
        return pollyQoSResiliencePipelineProvider;
    }

    private static DownstreamRoute DownstreamRouteFactory(string routeTemplate, bool inactiveExceptionsAllowedBeforeBreaking = false)
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(10000)
            .WithExceptionsAllowedBeforeBreaking(inactiveExceptionsAllowedBeforeBreaking ? 0 : 2)
            .WithDurationOfBreak(5000)
            .Build();

        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate(routeTemplate)
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue(routeTemplate)
            .Build();

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath)
            .Build();

        return route;
    }
}
