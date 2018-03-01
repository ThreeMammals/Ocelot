using System;
using System.Net.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Errors;
using Ocelot.Requester;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class DelegatingHandlerHandlerHouseTests
    {
        private readonly DelegatingHandlerHandlerHouse _house;
        private Mock<IDelegatingHandlerHandlerProviderFactory> _factory;
        private readonly Mock<IDelegatingHandlerHandlerProvider> _provider;
        private DownstreamReRoute _request;
        private Response<IDelegatingHandlerHandlerProvider> _result;

        public DelegatingHandlerHandlerHouseTests()
        {
            _provider = new Mock<IDelegatingHandlerHandlerProvider>();
            _factory = new Mock<IDelegatingHandlerHandlerProviderFactory>();
            _house = new DelegatingHandlerHandlerHouse(_factory.Object);
        }

        [Fact]
        public void should_create_and_store_provider()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("key").Build();

            this.Given(x => GivenTheRequest(reRoute))
                .And(x => GivenTheProviderReturns())
                .When(x => WhenIGet())
                .Then(x => ThenTheFactoryIsCalled(1))
                .And(x => ThenTheProviderIsNotNull())
                .BDDfy();
        }

        [Fact]
        public void should_get_provider()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("key").Build();

            this.Given(x => GivenTheRequest(reRoute))
                .And(x => GivenTheProviderReturns())
                .And(x => WhenIGet())
                .And(x => GivenTheFactoryIsCleared())
                .When(x => WhenIGet())
                .Then(x => ThenTheFactoryIsCalled(0))
                .And(x => ThenTheProviderIsNotNull())
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("key").Build();

            this.Given(x => GivenTheRequest(reRoute))
                .And(x => GivenTheProviderThrows())
                .When(x => WhenIGet())
                .And(x => ThenAnErrorIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_factory_errors()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("key").Build();

            this.Given(x => GivenTheRequest(reRoute))
                .And(x => GivenTheProviderReturnsError())
                .When(x => WhenIGet())
                .Then(x => ThenAnUnknownErrorIsReturned())
                .BDDfy();
        }

        private void ThenAnUnknownErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void ThenAnErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
            _result.Errors[0].ShouldBeOfType<UnableToFindDelegatingHandlerProviderError>();
        }

        private void GivenTheProviderThrows()
        {
            _factory.Setup(x => x.Get(It.IsAny<DownstreamReRoute>())).Throws<Exception>();
        }

        private void GivenTheFactoryIsCleared()
        {
            _factory = new Mock<IDelegatingHandlerHandlerProviderFactory>();
        }

        private void ThenTheProviderIsNotNull()
        {
            _result.Data.ShouldBe(_provider.Object);
        }

        private void WhenIGet()
        {
            _result = _house.Get(_request);
        }

        private void GivenTheRequest(DownstreamReRoute request)
        {
            _request = request;
        }

        private void GivenTheProviderReturns()
        {
            _factory.Setup(x => x.Get(It.IsAny<DownstreamReRoute>())).Returns(new OkResponse<IDelegatingHandlerHandlerProvider>(_provider.Object));
        }

        private void GivenTheProviderReturnsError()
        {
            _factory.Setup(x => x.Get(It.IsAny<DownstreamReRoute>())).Returns(new ErrorResponse<IDelegatingHandlerHandlerProvider>(It.IsAny<Error>()));
        }

        private void ThenTheFactoryIsCalled(int times)
        {
            _factory.Verify(x => x.Get(_request), Times.Exactly(times));
        }
    }
}
