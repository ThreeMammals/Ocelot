using Moq;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using TestStack.BDDfy;
using Xunit;
using Shouldly;

namespace Ocelot.UnitTests.Requester
{
    public class HttpClientHttpRequesterTest
    {
        private readonly Mock<IHttpClientCache> _cacheHandlers;
        private Mock<IDelegatingHandlerHandlerFactory> _house;
        private Response<HttpResponseMessage> _response;
        private readonly HttpClientHttpRequester _httpClientRequester;
        private DownstreamContext _request;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;

        public HttpClientHttpRequesterTest()
        {
            _house = new Mock<IDelegatingHandlerHandlerFactory>();
            _house.Setup(x => x.Get(It.IsAny<DownstreamReRoute>())).Returns(new OkResponse<List<Func<DelegatingHandler>>>(new List<Func<DelegatingHandler>>()));
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _loggerFactory
                .Setup(x => x.CreateLogger<HttpClientHttpRequester>())
                .Returns(_logger.Object);
            _cacheHandlers = new Mock<IHttpClientCache>();
            _httpClientRequester = new HttpClientHttpRequester(
                _loggerFactory.Object, 
                _cacheHandlers.Object, 
                _house.Object);            
        }

        [Fact]
        public void should_call_request_correctly()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false)).WithReRouteKey("").Build();

            var context = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamReRoute = reRoute,
                DownstreamRequest = new HttpRequestMessage() { RequestUri = new Uri("http://www.bbc.co.uk") },
            };

            this.Given(x=>x.GivenTheRequestIs(context))
                .When(x=>x.WhenIGetResponse())
                .Then(x => x.ThenTheResponseIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_call_request_unable_to_complete_request()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false)).WithReRouteKey("").Build();

            var context = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamReRoute = reRoute,
                DownstreamRequest = new HttpRequestMessage() { RequestUri = new Uri("http://localhost:60080") },
            };

            this.Given(x => x.GivenTheRequestIs(context))
                .When(x => x.WhenIGetResponse())
                .Then(x => x.ThenTheResponseIsCalledError())
                .BDDfy();
        }

        private void GivenTheRequestIs(DownstreamContext request)
        {
            _request = request;            
        }

        private void WhenIGetResponse()
        {
            _response = _httpClientRequester.GetResponse(_request).Result;
        }

        private void ThenTheResponseIsCalledCorrectly()
        {
            _response.IsError.ShouldBeFalse();
        }

        private void ThenTheResponseIsCalledError()
        {
            _response.IsError.ShouldBeTrue();
        }
    }  
}
