using Moq;

using Ocelot.Configuration.Builder;
using Ocelot.Logging;

using Ocelot.Provider.Polly;

using Shouldly;

using Xunit;

namespace Ocelot.UnitTests.Polly
{
    public class PollyQoSProviderTests
    {
        [Fact]
        public void should_build()
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
            pollyQoSProvider.CircuitBreaker.ShouldNotBeNull();
        }
    }
}
