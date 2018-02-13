using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using Ocelot.Requester;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class HttpClientBuilderTests
    {
        private readonly HttpClientBuilder _builder;
        private readonly Mock<IDelegatingHandlerHandlerHouse> _house;
        private readonly Mock<IDelegatingHandlerHandlerProvider> _provider;
        private IHttpClientBuilder _builderResult;
        private IHttpClient _httpClient;
        private HttpResponseMessage _response;
        private Ocelot.Request.Request _request;

        public HttpClientBuilderTests()
        {
            _provider = new Mock<IDelegatingHandlerHandlerProvider>();
            _house = new Mock<IDelegatingHandlerHandlerHouse>();
            _builder = new HttpClientBuilder(_house.Object);
        }

        [Fact]
        public void should_build_http_client()
        {
            this.Given(x => GivenTheProviderReturns())
                .And(x => GivenARequest())
                .And(x => GivenTheHouseReturns())
                .When(x => WhenIBuild())
                .Then(x => ThenTheHttpClientShouldNotBeNull())
                .BDDfy();
        }

        [Fact]
        public void should_call_delegating_handlers_in_order()
        {
            var fakeOne = new FakeDelegatingHandler();
            var fakeTwo = new FakeDelegatingHandler();

            var handlers = new List<Func<DelegatingHandler>>()
            { 
                () => fakeOne,
                () => fakeTwo
            };

            this.Given(x => GivenTheProviderReturns(handlers))
                .And(x => GivenARequest())
                .And(x => GivenTheHouseReturns())
                .And(x => WhenIBuild())
                .When(x => WhenICallTheClient())
                .Then(x => ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo))
                .And(x => ThenSomethingIsReturned())
                .BDDfy();
        }

        private void GivenARequest()
        {
            _request = new Ocelot.Request.Request(null, false, null, false, false, "", false);
        }

        private void GivenTheHouseReturns()
        {
            _house
                .Setup(x => x.Get(It.IsAny<Ocelot.Request.Request>()))
                .Returns(new OkResponse<IDelegatingHandlerHandlerProvider>(_provider.Object));
        }

        private void ThenSomethingIsReturned()
        {
            _response.ShouldNotBeNull();
        }

        private void WhenICallTheClient()
        {
            _response = _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com")).GetAwaiter().GetResult();
        }

        private void ThenTheFakeAreHandledInOrder(FakeDelegatingHandler fakeOne, FakeDelegatingHandler fakeTwo)
        {
            fakeOne.TimeCalled.ShouldBeGreaterThan(fakeTwo.TimeCalled);
        }

        private void GivenTheProviderReturns()
        {
            _provider
                .Setup(x => x.Get())
                .Returns(new List<Func<DelegatingHandler>>(){ () => new FakeDelegatingHandler()});
        }

        private void GivenTheProviderReturns(List<Func<DelegatingHandler>> handlers)
        {
            _provider
                .Setup(x => x.Get())
                .Returns(handlers);
        }

        private void WhenIBuild()
        {
            _httpClient = _builder.Create(_request);
        }

        private void ThenTheHttpClientShouldNotBeNull()
        {
            _httpClient.ShouldNotBeNull();
        }
    }
}
