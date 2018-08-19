/*
namespace Ocelot.UnitTests.Requester
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Logging;
    using Ocelot.Requester.QoS;
    using Shouldly;
    using System.Collections.Generic;
    using TestStack.BDDfy;
    using Xunit;

    public class QoSProviderFactoryTests
    {
        private readonly IQoSProviderFactory _factory;
        private DownstreamReRoute _reRoute;
        private IQoSProvider _result;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;

        public QoSProviderFactoryTests()
        {
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();
            _factory = new QoSProviderFactory(_loggerFactory.Object, provider);
        }

        [Fact]
        public void should_return_no_qos_provider()
        {
            var qosOptions = new QoSOptionsBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithUpstreamHttpMethod(new List<string> { "get" })
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheQoSProvider())
                .Then(x => x.ThenTheQoSProviderIsReturned<NoQoSProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_delegate_provider()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(100)
                .WithDurationOfBreak(100)
                .WithExceptionsAllowedBeforeBreaking(100)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithUpstreamHttpMethod(new List<string> { "get" })
                .WithQosOptions(qosOptions)
                .Build();

            this.Given(x => x.GivenAReRoute(reRoute))
                .When(x => x.WhenIGetTheQoSProvider())
                .Then(x => x.ThenTheQoSProviderIsReturned<FakeProvider>())
                .BDDfy();
        }

        private void GivenAReRoute(DownstreamReRoute reRoute)
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

    internal class FakeProvider : IQoSProvider
    {
        public T CircuitBreaker<T>()
        {
            throw new System.NotImplementedException();
        }
    }
}
*/
