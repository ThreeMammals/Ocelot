using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class HttpClientHttpRequesterTest
    {
        private readonly Mock<IHttpClientCache> _cacheHandlers;
        private Mock<IServiceProvider> _serviceProvider;
        private Response<HttpResponseMessage> _response;
        private readonly HttpClientHttpRequester _httpClientRequester;
        private Ocelot.Request.Request _request;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;

        public HttpClientHttpRequesterTest()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _loggerFactory
                .Setup(x => x.CreateLogger<HttpClientHttpRequester>())
                .Returns(_logger.Object);
            _cacheHandlers = new Mock<IHttpClientCache>();
            _httpClientRequester = new HttpClientHttpRequester(_loggerFactory.Object, _cacheHandlers.Object, _serviceProvider.Object);            
        }

        [Fact]
        public void should_call_request_correctly()
        {
            this.Given(x=>x.GivenTheRequestIs(new Ocelot.Request.Request(new HttpRequestMessage() {  RequestUri = new Uri("http://www.bbc.co.uk") }, false, new NoQoSProvider(), false, false, false)))
                .When(x=>x.WhenIGetResponse())
                .Then(x => x.ThenTheResponseIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_call_request_UnableToCompleteRequest()
        {
            this.Given(x => x.GivenTheRequestIs(new Ocelot.Request.Request(new HttpRequestMessage() { RequestUri = new Uri("http://localhost:60080") }, false, new NoQoSProvider(), false, false, false)))
                .When(x => x.WhenIGetResponse())
                .Then(x => x.ThenTheResponseIsCalledError())
                .BDDfy();
        }

        private void GivenTheRequestIs(Ocelot.Request.Request request)
        {
            _request = request;            
        }

        private void WhenIGetResponse()
        {
            _response = _httpClientRequester.GetResponse(_request).Result;
        }

        private void ThenTheResponseIsCalledCorrectly()
        {
            Assert.True(_response.IsError == false);
        }

        private void ThenTheResponseIsCalledError()
        {
            Assert.True(_response.IsError == true);
        }
    }  
}
