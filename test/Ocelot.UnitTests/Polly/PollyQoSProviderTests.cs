using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Polly.CircuitBreaker;

namespace Ocelot.UnitTests.Polly;

public class PollyQoSProviderTests
{
    [Fact]
    public void Should_build()
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(100)
            .WithExceptionsAllowedBeforeBreaking(1)
            .WithDurationOfBreak(200)
            .Build();
        var route = new DownstreamRouteBuilder().WithQosOptions(options)
            .Build();
        var factory = new Mock<IOcelotLoggerFactory>();
        var pollyQoSProvider = new PollyQoSProvider(factory.Object);
        var policy = pollyQoSProvider.GetCircuitBreaker(route).ShouldNotBeNull()
            .CircuitBreakerAsyncPolicy.ShouldNotBeNull();
        policy.ShouldNotBeNull();
    }

    [Fact]
    public void should_return_same_circuit_breaker_for_given_route()
    {
        var pollyQosProvider = PollyQoSProviderFactory();
        var circuitBreaker = CircuitBreakerFactory("/", pollyQosProvider);
        var circuitBreaker2 = CircuitBreakerFactory("/", pollyQosProvider);
        circuitBreaker.ShouldBe(circuitBreaker2);
    }

    [Fact]
    public void should_return_different_circuit_breaker_for_two_different_routes()
    {
        var pollyQosProvider = PollyQoSProviderFactory();
        var circuitBreaker = CircuitBreakerFactory("/", pollyQosProvider);
        var circuitBreaker2 = CircuitBreakerFactory("/test", pollyQosProvider);
        circuitBreaker.ShouldNotBe(circuitBreaker2);
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
    public async Task should_throw_broken_circuit_exception_after_two_exceptions(HttpStatusCode errorCode)
    {
        var circuitBreaker = CircuitBreakerFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(errorCode);
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    [Fact]
    public async Task should_not_throw_broken_circuit_exception_if_status_code_ok()
    {
        var circuitBreaker = CircuitBreakerFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
    }

    [Fact]
    public async Task should_throw_and_before_delay_should_not_allow_requests()
    {
        var circuitBreaker = CircuitBreakerFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(100);

        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    [Fact]
    public async Task should_throw_but_after_delay_should_allow_one_more_internal_server_error()
    {
        var circuitBreaker = CircuitBreakerFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
                       await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(200);

        Assert.Equal(HttpStatusCode.InternalServerError, (await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
    }

    [Fact]
    public async Task should_throw_but_after_delay_should_allow_one_more_internal_server_error_and_throw()
    {
        var circuitBreaker = CircuitBreakerFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(200);

        Assert.Equal(HttpStatusCode.InternalServerError, (await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    [Fact]
    public async Task should_throw_but_after_delay_should_allow_one_more_ok_request_and_put_counter_back_to_zero()
    {
        var circuitBreaker = CircuitBreakerFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(200);

        var response2 = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response2))).StatusCode);
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await circuitBreaker.CircuitBreakerAsyncPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    private PollyQoSProvider PollyQoSProviderFactory()
    {
        var factory = new Mock<IOcelotLoggerFactory>();
        factory.Setup(x => x.CreateLogger<PollyQoSProvider>())
            .Returns(new Mock<IOcelotLogger>().Object);

        var pollyQoSProvider = new PollyQoSProvider(factory.Object);
        return pollyQoSProvider;
    }

    private static CircuitBreaker<HttpResponseMessage> CircuitBreakerFactory(string routeTemplate, PollyQoSProvider pollyQoSProvider)
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(5000)
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithDurationOfBreak(200)
            .Build();

        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate(routeTemplate)
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue(routeTemplate).Build();

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath).Build();

        var circuitBreaker = pollyQoSProvider.GetCircuitBreaker(route).ShouldNotBeNull();
        circuitBreaker.ShouldNotBeNull();
        circuitBreaker.CircuitBreakerAsyncPolicy.ShouldNotBeNull();

        return circuitBreaker;
    }
}
