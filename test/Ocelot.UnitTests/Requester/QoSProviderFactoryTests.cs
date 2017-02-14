using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Requester.QoS;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class QoSProviderFactoryTests
    {
        private readonly IQoSProviderFactory _factory;
        private ReRoute _reRoute;
        private IQoSProvider _result;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;

        public QoSProviderFactoryTests()
        {
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _loggerFactory
                .Setup(x => x.CreateLogger<PollyQoSProvider>())
                .Returns(_logger.Object);
            _factory = new QoSProviderFactory(_loggerFactory.Object);
        }

        [Fact]
        public void should_return_no_qos_provider()
        {
            var reRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod("get")
                .WithIsQos(false)
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheQoSProvider())
                .Then(x => x.ThenTheQoSProviderIsReturned<NoQoSProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_polly_qos_provider()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(100)
                .WithDurationOfBreak(100)
                .WithExceptionsAllowedBeforeBreaking(100)
                .Build();

            var reRoute = new ReRouteBuilder()
               .WithUpstreamHttpMethod("get")
               .WithIsQos(true)
               .WithQosOptions(qosOptions)
               .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheQoSProvider())
                .Then(x => x.ThenTheQoSProviderIsReturned<PollyQoSProvider>())
                .BDDfy();
        }

        private void GivenAReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGetTheQoSProvider()
        {
            _result = _factory.Get(_reRoute);
        }

        private void ThenTheQoSProviderIsReturned<T>()
        {
            _result.ShouldBeOfType<T>();
        }
    }
}
