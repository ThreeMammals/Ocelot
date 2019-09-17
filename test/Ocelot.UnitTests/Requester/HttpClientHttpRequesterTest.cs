using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester;
using Ocelot.Responses;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class HttpClientHttpRequesterTest
    {
        private readonly Mock<IHttpClientCache> _cacheHandlers;
        private readonly Mock<IDelegatingHandlerHandlerFactory> _factory;
        private Response<HttpResponseMessage> _response;
        private readonly HttpClientHttpRequester _httpClientRequester;
        private DownstreamContext _request;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private Mock<IExceptionToErrorMapper> _mapper;

        public HttpClientHttpRequesterTest()
        {
            _factory = new Mock<IDelegatingHandlerHandlerFactory>();
            _factory.Setup(x => x.Get(It.IsAny<DownstreamReRoute>())).Returns(new OkResponse<List<Func<DelegatingHandler>>>(new List<Func<DelegatingHandler>>()));
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _loggerFactory
                .Setup(x => x.CreateLogger<HttpClientHttpRequester>())
                .Returns(_logger.Object);
            _cacheHandlers = new Mock<IHttpClientCache>();
            _mapper = new Mock<IExceptionToErrorMapper>();
            _httpClientRequester = new HttpClientHttpRequester(
                _loggerFactory.Object,
                _cacheHandlers.Object,
                _factory.Object,
                _mapper.Object);
        }

        [Fact]
        public void should_call_request_correctly()
        {
            var upstreamTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue("").Build();

            var qosOptions = new QoSOptionsBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false, true))
                .WithLoadBalancerKey("")
                .WithUpstreamPathTemplate(upstreamTemplate)
                .WithQosOptions(new QoSOptionsBuilder().Build())
                .Build();

            var context = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamReRoute = reRoute,
                DownstreamRequest = new DownstreamRequest(new HttpRequestMessage() { RequestUri = new Uri("http://www.bbc.co.uk") }),
            };

            this.Given(x => x.GivenTheRequestIs(context))
                .And(x => GivenTheHouseReturnsOkHandler())
                .When(x => x.WhenIGetResponse())
                .Then(x => x.ThenTheResponseIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_call_request_unable_to_complete_request()
        {
            var upstreamTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue("").Build();

            var qosOptions = new QoSOptionsBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false, true))
                .WithLoadBalancerKey("")
                .WithUpstreamPathTemplate(upstreamTemplate)
                .WithQosOptions(new QoSOptionsBuilder().Build())
                .Build();

            var context = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamReRoute = reRoute,
                DownstreamRequest = new DownstreamRequest(new HttpRequestMessage() { RequestUri = new Uri("http://localhost:60080") }),
            };

            this.Given(x => x.GivenTheRequestIs(context))
                .When(x => x.WhenIGetResponse())
                .Then(x => x.ThenTheResponseIsCalledError())
                .BDDfy();
        }

        [Fact]
        public void http_client_request_times_out()
        {
            var upstreamTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue("").Build();

            var qosOptions = new QoSOptionsBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(false, false, false, true))
                .WithLoadBalancerKey("")
                .WithUpstreamPathTemplate(upstreamTemplate)
                .WithQosOptions(new QoSOptionsBuilder().WithTimeoutValue(1).Build())
                .Build();

            var context = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamReRoute = reRoute,
                DownstreamRequest = new DownstreamRequest(new HttpRequestMessage() { RequestUri = new Uri("http://localhost:60080") }),
            };

            this.Given(_ => GivenTheRequestIs(context))
                .And(_ => GivenTheHouseReturnsTimeoutHandler())
                .When(_ => WhenIGetResponse())
                .Then(_ => ThenTheResponseIsCalledError())
                .And(_ => ThenTheErrorIsTimeout())
                .BDDfy();
        }

        private void GivenTheRequestIs(DownstreamContext request)
        {
            _request = request;
        }

        private void WhenIGetResponse()
        {
            _response = _httpClientRequester.GetResponse(_request).GetAwaiter().GetResult();
        }

        private void ThenTheResponseIsCalledCorrectly()
        {
            _response.IsError.ShouldBeFalse();
        }

        private void ThenTheResponseIsCalledError()
        {
            _response.IsError.ShouldBeTrue();
        }

        private void ThenTheErrorIsTimeout()
        {
            _mapper.Verify(x => x.Map(It.IsAny<Exception>()), Times.Once);
            _response.Errors[0].ShouldBeOfType<UnableToCompleteRequestError>();
        }

        private void GivenTheHouseReturnsOkHandler()
        {
            var handlers = new List<Func<DelegatingHandler>>
            {
                () => new OkDelegatingHandler()
            };

            _factory.Setup(x => x.Get(It.IsAny<DownstreamReRoute>())).Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));
        }

        private void GivenTheHouseReturnsTimeoutHandler()
        {
            var handlers = new List<Func<DelegatingHandler>>
            {
                () => new TimeoutDelegatingHandler()
            };

            _factory.Setup(x => x.Get(It.IsAny<DownstreamReRoute>())).Returns(new OkResponse<List<Func<DelegatingHandler>>>(handlers));

            _mapper.Setup(x => x.Map(It.IsAny<Exception>())).Returns(new UnableToCompleteRequestError(new Exception()));
        }

        private class OkDelegatingHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage());
            }
        }

        private class TimeoutDelegatingHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(100000, cancellationToken);
                return new HttpResponseMessage();
            }
        }
    }
}
