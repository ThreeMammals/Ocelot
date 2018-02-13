using System;
using System.Net.Http;
using Moq;
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
        private Ocelot.Request.Request _request;
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
            var request = new Ocelot.Request.Request(new HttpRequestMessage(), true, null, true, true, "key", false);

            this.Given(x => GivenTheRequest(request))
                .And(x => GivenTheProviderReturns())
                .When(x => WhenIGet())
                .Then(x => ThenTheFactoryIsCalled(1))
                .And(x => ThenTheProviderIsNotNull())
                .BDDfy();
        }

        [Fact]
        public void should_get_provider()
        {
            var request = new Ocelot.Request.Request(new HttpRequestMessage(), true, null, true, true, "key", false);

            this.Given(x => GivenTheRequest(request))
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
            var request = new Ocelot.Request.Request(new HttpRequestMessage(), true, null, true, true, "key", false);

            this.Given(x => GivenTheRequest(request))
                .And(x => GivenTheProviderThrows())
                .When(x => WhenIGet())
                .And(x => ThenAnErrorIsReturned())
                .BDDfy();
        }

        private void ThenAnErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
            _result.Errors[0].ShouldBeOfType<UnableToFindDelegatingHandlerProviderError>();
        }

        private void GivenTheProviderThrows()
        {
            _factory.Setup(x => x.Get(It.IsAny<Ocelot.Request.Request>())).Throws<Exception>();
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

        private void GivenTheRequest(Ocelot.Request.Request request)
        {
            _request = request;
        }

        private void GivenTheProviderReturns()
        {
            _factory.Setup(x => x.Get(It.IsAny<Ocelot.Request.Request>())).Returns(_provider.Object);
        }

        private void ThenTheFactoryIsCalled(int times)
        {
            _factory.Verify(x => x.Get(_request), Times.Exactly(times));
        }
    }
}
