using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
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
            var pollyQoSProvider = new PollyQoSProvider(route, factory.Object);
            var policies = pollyQoSProvider.CircuitBreaker.ShouldNotBeNull()
                .Policies.ShouldNotBeNull();
            policies.Length.ShouldBeGreaterThan(0);
            policies.ShouldContain(p => p is AsyncCircuitBreakerPolicy);
            policies.ShouldContain(p => p is AsyncTimeoutPolicy);
        }
    }
}
