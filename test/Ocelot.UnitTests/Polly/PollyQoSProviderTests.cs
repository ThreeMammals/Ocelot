using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.UnitTests.Polly
{
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
            var options = new QoSOptionsBuilder()
                .WithTimeoutValue(100)
                .WithExceptionsAllowedBeforeBreaking(2)
                .WithDurationOfBreak(200)
                .Build();

            var upstreamPath = new UpstreamPathTemplateBuilder()
                .WithTemplate("/")
                .WithContainsQueryString(false)
                .WithPriority(1)
                .WithOriginalValue("/").Build();

            var route = new DownstreamRouteBuilder()
                .WithQosOptions(options)
                .WithUpstreamPathTemplate(upstreamPath).Build();

            var factory = new Mock<IOcelotLoggerFactory>();
            var pollyQoSProvider = new PollyQoSProvider(factory.Object);
            var circuitBreaker = pollyQoSProvider.GetCircuitBreaker(route).ShouldNotBeNull();
            var circuitBreaker2 = pollyQoSProvider.GetCircuitBreaker(route).ShouldNotBeNull();
            circuitBreaker.ShouldBe(circuitBreaker2);
        }

        [Fact]
        public void should_return_different_circuit_breaker_for_two_different_routes()
        {
            var options = new QoSOptionsBuilder()
                .WithTimeoutValue(100)
                .WithExceptionsAllowedBeforeBreaking(2)
                .WithDurationOfBreak(200)
                .Build();

            var upstreamPath = new UpstreamPathTemplateBuilder()
                .WithTemplate("/")
                .WithContainsQueryString(false)
                .WithPriority(1)
                .WithOriginalValue("/").Build();

            var route = new DownstreamRouteBuilder()
                .WithQosOptions(options)
                .WithUpstreamPathTemplate(upstreamPath).Build();

            var options2 = new QoSOptionsBuilder()
                .WithTimeoutValue(100)
                .WithExceptionsAllowedBeforeBreaking(2)
                .WithDurationOfBreak(200)
                .Build();

            var upstreamPath2 = new UpstreamPathTemplateBuilder()
                .WithTemplate("/test2")
                .WithContainsQueryString(false)
                .WithPriority(1)
                .WithOriginalValue("/test2").Build();

            var route2 = new DownstreamRouteBuilder()
                .WithQosOptions(options2)
                .WithUpstreamPathTemplate(upstreamPath2).Build();

            var factory = new Mock<IOcelotLoggerFactory>();
            var pollyQoSProvider = new PollyQoSProvider(factory.Object);
            var circuitBreaker = pollyQoSProvider.GetCircuitBreaker(route).ShouldNotBeNull();
            var circuitBreaker2 = pollyQoSProvider.GetCircuitBreaker(route2).ShouldNotBeNull();
            circuitBreaker.ShouldNotBe(circuitBreaker2);
        }
    }
}
