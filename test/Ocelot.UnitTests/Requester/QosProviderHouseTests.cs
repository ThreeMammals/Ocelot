using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class QosProviderHouseTests
    {
        private IQoSProvider _qoSProvider;
        private readonly QosProviderHouse _qosProviderHouse;
        private Response<IQoSProvider> _getResult;
        private DownstreamReRoute _reRoute;
        private readonly Mock<IQoSProviderFactory> _factory;

        public QosProviderHouseTests()
        {
            _factory = new Mock<IQoSProviderFactory>();
            _qosProviderHouse = new QosProviderHouse(_factory.Object);
        }

        [Fact]
        public void should_store_qos_provider_on_first_request()
        {
            var reRoute = new DownstreamReRouteBuilder().WithQosKey("test").Build();

            this.Given(x => x.GivenThereIsAQoSProvider(reRoute, new FakeQoSProvider()))
                .Then(x => x.ThenItIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_not_store_qos_provider_on_first_request()
        {
            var reRoute = new DownstreamReRouteBuilder().WithQosKey("test").Build();

            this.Given(x => x.GivenThereIsAQoSProvider(reRoute, new FakeQoSProvider()))
                .When(x => x.WhenWeGetTheQoSProvider(reRoute))
                .Then(x => x.ThenItIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_store_qos_providers_by_key()
        {
            var reRoute = new DownstreamReRouteBuilder().WithQosKey("test").Build();
            var reRouteTwo = new DownstreamReRouteBuilder().WithQosKey("testTwo").Build();

            this.Given(x => x.GivenThereIsAQoSProvider(reRoute, new FakeQoSProvider()))
                .And(x => x.GivenThereIsAQoSProvider(reRouteTwo, new FakePollyQoSProvider()))
                .When(x => x.WhenWeGetTheQoSProvider(reRoute))
                .Then(x => x.ThenTheQoSProviderIs<FakeQoSProvider>())
                .When(x => x.WhenWeGetTheQoSProvider(reRouteTwo))
                .Then(x => x.ThenTheQoSProviderIs<FakePollyQoSProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_no_qos_provider_with_key()
        {
            var reRoute = new DownstreamReRouteBuilder().Build();

            this.When(x => x.WhenWeGetTheQoSProvider(reRoute))
            .Then(x => x.ThenAnErrorIsReturned())
            .BDDfy();
        }

        [Fact]
        public void should_get_new_qos_provider_if_reroute_qos_provider_has_changed()
        {
            var reRoute = new DownstreamReRouteBuilder().WithQosKey("test").Build();

            var reRouteTwo = new DownstreamReRouteBuilder().WithQosKey("test").WithIsQos(true).Build();

            this.Given(x => x.GivenThereIsAQoSProvider(reRoute, new FakeQoSProvider()))
                .When(x => x.WhenWeGetTheQoSProvider(reRoute))
                .Then(x => x.ThenTheQoSProviderIs<FakeQoSProvider>())
                .When(x => x.WhenIGetTheReRouteWithTheSameKeyButDifferentQosProvider(reRouteTwo))
                .Then(x => x.ThenTheQoSProviderIs<FakePollyQoSProvider>())
                .BDDfy();
        }

        private void WhenIGetTheReRouteWithTheSameKeyButDifferentQosProvider(DownstreamReRoute reRoute)
        {
            _reRoute = reRoute;
            _factory.Setup(x => x.Get(_reRoute)).Returns(new FakePollyQoSProvider());
            _getResult = _qosProviderHouse.Get(_reRoute);
        }

        private void ThenAnErrorIsReturned()
        {
            _getResult.IsError.ShouldBeTrue();
            _getResult.Errors[0].ShouldBeOfType<UnableToFindQoSProviderError>();
        }

        private void ThenTheQoSProviderIs<T>()
        {
            _getResult.Data.ShouldBeOfType<T>();
        }

        private void ThenItIsAdded()
        {
            _getResult.IsError.ShouldBe(false);
            _getResult.ShouldBeOfType<OkResponse<IQoSProvider>>();
            _factory.Verify(x => x.Get(_reRoute), Times.Once);
            _getResult.Data.ShouldBe(_qoSProvider);
        }

        private void GivenThereIsAQoSProvider(DownstreamReRoute reRoute, IQoSProvider qoSProvider)
        {
            _reRoute = reRoute;
            _qoSProvider = qoSProvider;
            _factory.Setup(x => x.Get(_reRoute)).Returns(_qoSProvider);
            _getResult = _qosProviderHouse.Get(reRoute);
        }

        private void WhenWeGetTheQoSProvider(DownstreamReRoute reRoute)
        {
            _getResult = _qosProviderHouse.Get(reRoute);
        }

        private void ThenItIsReturned()
        {
            _getResult.Data.ShouldBe(_qoSProvider);
            _factory.Verify(x => x.Get(_reRoute), Times.Once);
        }

        class FakeQoSProvider : IQoSProvider
        {
            public CircuitBreaker CircuitBreaker { get; }
        }

        class FakePollyQoSProvider : IQoSProvider
        {
            public CircuitBreaker CircuitBreaker { get; }
        }
    }
}
