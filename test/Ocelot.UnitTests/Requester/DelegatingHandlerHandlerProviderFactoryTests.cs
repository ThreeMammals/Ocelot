using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class DelegatingHandlerHandlerProviderFactoryTests
    {
        private readonly DelegatingHandlerHandlerProviderFactory _factory;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private DownstreamReRoute _request;
        private Response<IDelegatingHandlerHandlerProvider> _provider;
        private readonly Mock<IDelegatingHandlerHandlerProvider> _allRoutesProvider;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly Mock<ITracingHandlerFactory> _tracingFactory;

        public DelegatingHandlerHandlerProviderFactoryTests()
        {
            _tracingFactory = new Mock<ITracingHandlerFactory>();
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _allRoutesProvider = new Mock<IDelegatingHandlerHandlerProvider>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _factory = new DelegatingHandlerHandlerProviderFactory(_loggerFactory.Object, _allRoutesProvider.Object, _tracingFactory.Object, _qosProviderHouse.Object);
        }

        private void GivenTheQosProviderHouseReturns(Response<IQoSProvider> qosProvider)
        {
            _qosProviderHouse
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(qosProvider);
        }


        [Fact]
        public void should_all_from_all_routes_provider_and_qos()
        {
            var handlers = new List<Func<DelegatingHandler>>
            {
                () => new FakeDelegatingHandler(0),
                () => new FakeDelegatingHandler(1)
            };

            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheAllRoutesProviderReturns(handlers))
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(3))
                .And(x => ThenTheDelegatesAreAddedCorrectly())
                .And(x => ThenItIsPolly(2))
                .BDDfy();
        }

        [Fact]
        public void should_return_provider_with_no_delegates()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheAllRoutesProviderReturns())
                .When(x => WhenIGet())
                .Then(x => ThenNoDelegatesAreInTheProvider())
                .BDDfy();
        }

        [Fact]
        public void should_return_provider_with_qos_delegate()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheAllRoutesProviderReturns())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(1))
                .And(x => ThenItIsPolly(0))
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new ErrorResponse<IQoSProvider>(It.IsAny<Error>())))
                .And(x => GivenTheAllRoutesProviderReturns())
                .When(x => WhenIGet())
                .Then(x => ThenAnErrorIsReturned())
                .BDDfy();
        }

        private void ThenAnErrorIsReturned()
        {
            _provider.IsError.ShouldBeTrue();
        }

        private void ThenTheDelegatesAreAddedCorrectly()
        {
            var delegates = _provider.Data.Get();
            var del = delegates[0].Invoke();
            var handler = (FakeDelegatingHandler) del;
            handler.Order.ShouldBe(0);

            del = delegates[1].Invoke();
            handler = (FakeDelegatingHandler)del;
            handler.Order.ShouldBe(1);
        }

        private void GivenTheAllRoutesProviderReturns()
        {
            _allRoutesProvider.Setup(x => x.Get()).Returns(new List<Func<DelegatingHandler>>());
        }

        private void GivenTheAllRoutesProviderReturns(List<Func<DelegatingHandler>> handlers)
        {
            _allRoutesProvider.Setup(x => x.Get()).Returns(handlers);
        }

        private void ThenItIsPolly(int i)
        {
            var delegates = _provider.Data.Get();
            var del = delegates[i].Invoke();
            del.ShouldBeOfType<PollyCircuitBreakingDelegatingHandler>();
        }

        private void ThenThereIsDelegatesInProvider(int count)
        {
            _provider.ShouldNotBeNull();
            _provider.Data.Get().Count.ShouldBe(count);
        }

        private void GivenTheFollowingRequest(DownstreamReRoute request)
        {
            _request = request;
        }

        private void WhenIGet()
        {
            _provider = _factory.Get(_request);
        }

        private void ThenNoDelegatesAreInTheProvider()
        {
            _provider.ShouldNotBeNull();
            _provider.Data.Get().Count.ShouldBe(0);
        }
    }
}
