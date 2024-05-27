using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Testing;
using Polly.Timeout;

namespace Ocelot.UnitTests.Polly;

public class PollyQoSResiliencePipelineProviderTests
{
    [Fact]
    public void ShouldBuild()
    {
        // Arrange
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(1000) // 10ms, minimum required by Polly
            .WithExceptionsAllowedBeforeBreaking(2) // 2 is the minimum required by Polly
            .WithDurationOfBreak(QoSOptions.LowBreakDuration + 1) // 0.5s, minimum required by Polly
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var registry = new ResiliencePipelineRegistry<OcelotResiliencePipelineKey>();
        var provider = new PollyQoSResiliencePipelineProvider(loggerFactoryMock.Object, registry);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        resiliencePipeline.ShouldBeOfType<ResiliencePipeline<HttpResponseMessage>>();
        resiliencePipeline.ShouldNotBe(ResiliencePipeline<HttpResponseMessage>.Empty);
    }

    [Fact]
    [Trait("Bug", "2085")]
    public void ShouldNotBuild_ReturnedEmpty()
    {
        // Arrange
        var options = new QoSOptionsBuilder().Build(); // empty options
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var registry = new ResiliencePipelineRegistry<OcelotResiliencePipelineKey>();
        var provider = new PollyQoSResiliencePipelineProvider(loggerFactoryMock.Object, registry);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        resiliencePipeline.ShouldBeOfType<ResiliencePipeline<HttpResponseMessage>>();
        resiliencePipeline.ShouldBe(ResiliencePipeline<HttpResponseMessage>.Empty);
    }

    [Theory]
    [Trait("Bug", "2085")]
    [InlineData(0, QoSOptions.DefaultBreakDuration)] // default
    [InlineData(QoSOptions.LowBreakDuration - 1, QoSOptions.DefaultBreakDuration)] // default
    [InlineData(QoSOptions.LowBreakDuration, QoSOptions.DefaultBreakDuration)] // default
    [InlineData(QoSOptions.LowBreakDuration + 1, QoSOptions.LowBreakDuration + 1)] // not default, exact
    public void ShouldBuild_WithDefaultBreakDuration(int durationOfBreak, int expectedMillisecons)
    {
        // Arrange
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(1000) // 10ms, minimum required by Polly
            .WithExceptionsAllowedBeforeBreaking(2) // 2 is the minimum required by Polly
            .WithDurationOfBreak(durationOfBreak) // 0.5s, minimum required by Polly
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var registry = new ResiliencePipelineRegistry<OcelotResiliencePipelineKey>();
        var provider = new PollyQoSResiliencePipelineProvider(loggerFactoryMock.Object, registry);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        resiliencePipeline.ShouldBeOfType<ResiliencePipeline<HttpResponseMessage>>();
        resiliencePipeline.ShouldNotBe(ResiliencePipeline<HttpResponseMessage>.Empty);
        var descriptor = resiliencePipeline.GetPipelineDescriptor();
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.Count.ShouldBe(2);
        descriptor.Strategies[0].Options.ShouldBeOfType<CircuitBreakerStrategyOptions<HttpResponseMessage>>();
        descriptor.Strategies[1].Options.ShouldBeOfType<TimeoutStrategyOptions>();
        var strategyOptions = descriptor.Strategies[0].Options as CircuitBreakerStrategyOptions<HttpResponseMessage>;
        strategyOptions.ShouldNotBeNull();
        strategyOptions.BreakDuration.ShouldBe(TimeSpan.FromMilliseconds(expectedMillisecons));
    }

    [Fact]
    public void Should_return_same_circuit_breaker_for_given_route()
    {
        // Arrange
        var provider = GivenProvider();
        var route1 = GivenDownstreamRoute("/");
        var route2 = GivenDownstreamRoute("/");

        // Act
        var resiliencePipeline1 = provider.GetResiliencePipeline(route1);
        var resiliencePipeline2 = provider.GetResiliencePipeline(route2);

        // Assert
        resiliencePipeline1.ShouldBe(resiliencePipeline2);

        // Act 2
        var resiliencePipeline3 = provider.GetResiliencePipeline(route1);

        // Assert 2
        resiliencePipeline3.ShouldBe(resiliencePipeline1);
        resiliencePipeline3.ShouldBe(resiliencePipeline2);
    }

    [Fact]
    public void Should_return_different_circuit_breaker_for_two_different_routes()
    {
        // Arrange
        var provider = GivenProvider();
        var route1 = GivenDownstreamRoute("/");
        var route2 = GivenDownstreamRoute("/test");

        // Act
        var resiliencePipeline1 = provider.GetResiliencePipeline(route1);
        var resiliencePipeline2 = provider.GetResiliencePipeline(route2);

        // Assert
        resiliencePipeline1.ShouldNotBe(resiliencePipeline2);
    }

    [Fact]
    [Trait("Bug", "2085")]
    public void ShouldBuild_ContainsTwoStrategies()
    {
        var pollyQoSResiliencePipelineProvider = GivenProvider();

        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);
        resiliencePipeline.ShouldNotBeNull();

        var descriptor = resiliencePipeline.GetPipelineDescriptor();
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.Count.ShouldBe(2);
        descriptor.Strategies[0].Options.ShouldBeOfType<CircuitBreakerStrategyOptions<HttpResponseMessage>>();
        descriptor.Strategies[1].Options.ShouldBeOfType<TimeoutStrategyOptions>();
    }

    [Fact]
    public void Should_build_and_contains_one_policy_when_with_exceptions_allowed_before_breaking_is_zero()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/", true); // get route with 0 exceptions allowed before breaking

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.GetPipelineDescriptor();

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.Count.ShouldBe(1);
        descriptor.Strategies.Single().Options.ShouldBeOfType<TimeoutStrategyOptions>();
    }

    [Fact]
    [Trait("Bug", "2085")]
    public async Task Should_throw_after_timeout()
    {
        // Arrange
        var provider = GivenProvider();
        const int timeOut = 1000;
        var route = GivenDownstreamRoute("/", false, timeOut);
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var cancellationTokenSource = new CancellationTokenSource();

        // Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>

            // Act
            await resiliencePipeline.ExecuteAsync(async (cancellationToken) =>
            {
                await Task.Delay(timeOut + 500, cancellationToken); // add 500ms to make sure it's timed out
                return response;
            },
            cancellationTokenSource.Token));
    }

    [Fact]
    [Trait("Bug", "2085")]
    public async Task Should_not_throw_before_timeout()
    {
        // Arrange
        var provider = GivenProvider();
        const int timeOut = 1000;
        var route = GivenDownstreamRoute("/", false, timeOut);
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            await Task.Delay(timeOut - 100, cancellationToken); // subtract 100ms to make sure it's not timed out
            return response;
        }, cancellationTokenSource.Token);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
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
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(errorCode);

        // Act
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_not_throw_broken_circuit_exception_if_status_code_ok()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act, Assert
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
    }

    [Fact]
    public async Task Should_throw_and_before_delay_should_not_allow_requests()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(200);

        // Act, Assert 2
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_internal_server_error()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(6000);

        // Act 2
        var response2 = await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Assert 2
        Assert.Equal(HttpStatusCode.InternalServerError, response2.StatusCode);
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_internal_server_error_and_throw()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(6000);

        // Act, Assert 2
        Assert.Equal(HttpStatusCode.InternalServerError, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);

        // Act, Assert 3
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_ok_request_and_put_counter_back_to_zero()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(10000);

        // Act, Assert 2
        var response2 = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response2))).StatusCode);

        // Act, Assert 3
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    private static PollyQoSResiliencePipelineProvider GivenProvider()
    {
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        loggerFactoryMock
            .Setup(x => x.CreateLogger<PollyQoSResiliencePipelineProvider>())
            .Returns(new Mock<IOcelotLogger>().Object);
        var registry = new ResiliencePipelineRegistry<OcelotResiliencePipelineKey>();
        return new PollyQoSResiliencePipelineProvider(loggerFactoryMock.Object, registry);
    }

    private static DownstreamRoute GivenDownstreamRoute(string routeTemplate, bool inactiveExceptionsAllowedBeforeBreaking = false, int timeOut = 10000)
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(timeOut)
            .WithExceptionsAllowedBeforeBreaking(inactiveExceptionsAllowedBeforeBreaking ? 0 : 2)
            .WithDurationOfBreak(5000)
            .Build();

        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate(routeTemplate)
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue(routeTemplate)
            .Build();

        return new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath)
            .Build();
    }
}
