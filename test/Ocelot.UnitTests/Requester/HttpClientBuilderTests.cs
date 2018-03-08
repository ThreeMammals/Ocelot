using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
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
        private readonly Mock<IDelegatingHandlerHandlerFactory> _factory;
        private IHttpClient _httpClient;
        private HttpResponseMessage _response;
        private DownstreamReRoute _request;

        public HttpClientBuilderTests()
        {
            _factory = new Mock<IDelegatingHandlerHandlerFactory>();
            _builder = new HttpClientBuilder(_factory.Object);
        }

        [Fact]
        public void should_build_http_client()
        {
            this.Given(x => GivenTheFactoryReturns())
                .And(x => GivenARequest())
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

            this.Given(x => GivenTheFactoryReturns(handlers))
                .And(x => GivenARequest())
                .And(x => WhenIBuild())
                .When(x => WhenICallTheClient())
                .Then(x => ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo))
                .And(x => ThenSomethingIsReturned())
                .BDDfy();
        }

        private void GivenARequest()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false)).WithReRouteKey("").Build();

            _request = reRoute;
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

        private void GivenTheFactoryReturns()
        {
            var handlers = new List<Func<DelegatingHandler>>(){ () => new FakeDelegatingHandler()};

            _factory
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
        }

        private void GivenTheFactoryReturns(List<Func<DelegatingHandler>> handlers)
        {
             _factory
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
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
