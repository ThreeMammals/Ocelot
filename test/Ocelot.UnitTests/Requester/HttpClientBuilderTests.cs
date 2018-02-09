using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class HttpClientBuilderTests
    {
        private HttpClientBuilder _builder;
        private Mock<IDelegatingHandlerHandlerProvider> _provider;
        private Mock<IQoSProvider> _qosProvider;
        private Mock<IOcelotLogger> _logger;
        private IHttpClientBuilder _builderResult;
        private IHttpClient _httpClient;
        private HttpResponseMessage _response;

        public HttpClientBuilderTests()
        {
            _logger = new Mock<IOcelotLogger>();
            _qosProvider = new Mock<IQoSProvider>();
            _provider = new Mock<IDelegatingHandlerHandlerProvider>();
            _builder = new HttpClientBuilder(_provider.Object);
        }

        [Fact]
        public void should_build_http_client()
        {
            this.Given(x => GivenTheProviderReturns())
                .When(x => WhenIBuild())
                .Then(x => ThenTheHttpClientShouldNotBeNull())
                .BDDfy();
        }

        [Fact]
        public void should_add_qos()
        {
            this.When(x => WhenIAddQos())
                .Then(x => ThenTheBuilderShouldNotBeNull())
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
                .And(x => WhenIBuild())
                .When(x => WhenICallTheClient())
                .Then(x => ThenTheFakeAreHandledInOrder(fakeOne, fakeTwo))
                .And(x => ThenSomethingIsReturned())
                .BDDfy();
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
            _httpClient = _builder.Create(false, false);
        }

        private void ThenTheHttpClientShouldNotBeNull()
        {
            _httpClient.ShouldNotBeNull();
        }

        private void WhenIAddQos()
        {
           _builderResult = _builder.WithQos(_qosProvider.Object, _logger.Object);
        }

        private void ThenTheBuilderShouldNotBeNull()
        {
            _builderResult.ShouldNotBeNull();
        }
    }
}