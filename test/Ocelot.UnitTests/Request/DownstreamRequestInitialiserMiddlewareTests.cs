using Ocelot.Middleware;

namespace Ocelot.UnitTests.Request
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Infrastructure;
    using Ocelot.Logging;
    using Ocelot.Request.Creator;
    using Ocelot.Request.Mapper;
    using Ocelot.Request.Middleware;
    using Ocelot.Responses;
    using Shouldly;
    using System.Net.Http;
    using TestStack.BDDfy;
    using Xunit;

    public class DownstreamRequestInitialiserMiddlewareTests
    {
        private readonly DownstreamRequestInitialiserMiddleware _middleware;

        private readonly Mock<HttpContext> _httpContext;

        private readonly Mock<HttpRequest> _httpRequest;

        private readonly Mock<OcelotRequestDelegate> _next;

        private readonly Mock<IRequestMapper> _requestMapper;

        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;

        private readonly Mock<IOcelotLogger> _logger;

        private Response<HttpRequestMessage> _mappedRequest;
        private DownstreamContext _downstreamContext;

        public DownstreamRequestInitialiserMiddlewareTests()
        {
            _httpContext = new Mock<HttpContext>();
            _httpRequest = new Mock<HttpRequest>();
            _requestMapper = new Mock<IRequestMapper>();
            _next = new Mock<OcelotRequestDelegate>();
            _logger = new Mock<IOcelotLogger>();

            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _loggerFactory
                .Setup(lf => lf.CreateLogger<DownstreamRequestInitialiserMiddleware>())
                .Returns(_logger.Object);

            _middleware = new DownstreamRequestInitialiserMiddleware(
                _next.Object,
                _loggerFactory.Object,
                _requestMapper.Object,
                new DownstreamRequestCreator(new FrameworkDescription()));

            _downstreamContext = new DownstreamContext(_httpContext.Object);
        }

        [Fact]
        public void Should_handle_valid_httpRequest()
        {
            this.Given(_ => GivenTheHttpContextContainsARequest())
                .And(_ => GivenTheMapperWillReturnAMappedRequest())
                .When(_ => WhenTheMiddlewareIsInvoked())
                .Then(_ => ThenTheContexRequestIsMappedToADownstreamRequest())
                .And(_ => ThenTheDownstreamRequestIsStored())
                .And(_ => ThenTheNextMiddlewareIsInvoked())
                .BDDfy();
        }

        [Fact]
        public void Should_handle_mapping_failure()
        {
            this.Given(_ => GivenTheHttpContextContainsARequest())
                .And(_ => GivenTheMapperWillReturnAnError())
                .When(_ => WhenTheMiddlewareIsInvoked())
                .And(_ => ThenTheDownstreamRequestIsNotStored())
                .And(_ => ThenAPipelineErrorIsStored())
                .And(_ => ThenTheNextMiddlewareIsNotInvoked())
                .BDDfy();
        }

        private void GivenTheHttpContextContainsARequest()
        {
            _httpContext
                .Setup(hc => hc.Request)
                .Returns(_httpRequest.Object);
        }

        private void GivenTheMapperWillReturnAMappedRequest()
        {
            _mappedRequest = new OkResponse<HttpRequestMessage>(new HttpRequestMessage(HttpMethod.Get, "http://www.bbc.co.uk"));

            _requestMapper
                .Setup(rm => rm.Map(It.IsAny<HttpRequest>()))
                .ReturnsAsync(_mappedRequest);
        }

        private void GivenTheMapperWillReturnAnError()
        {
            _mappedRequest = new ErrorResponse<HttpRequestMessage>(new UnmappableRequestError(new System.Exception("boooom!")));

            _requestMapper
                .Setup(rm => rm.Map(It.IsAny<HttpRequest>()))
                .ReturnsAsync(_mappedRequest);
        }

        private void WhenTheMiddlewareIsInvoked()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void ThenTheContexRequestIsMappedToADownstreamRequest()
        {
            _requestMapper.Verify(rm => rm.Map(_httpRequest.Object), Times.Once);
        }

        private void ThenTheDownstreamRequestIsStored()
        {
            _downstreamContext.DownstreamRequest.ShouldNotBeNull();
        }

        private void ThenTheDownstreamRequestIsNotStored()
        {
            _downstreamContext.DownstreamRequest.ShouldBeNull();
        }

        private void ThenAPipelineErrorIsStored()
        {
            _downstreamContext.IsError.ShouldBeTrue();
            _downstreamContext.Errors.ShouldBe(_mappedRequest.Errors);
        }

        private void ThenTheNextMiddlewareIsInvoked()
        {
            _next.Verify(n => n(_downstreamContext), Times.Once);
        }

        private void ThenTheNextMiddlewareIsNotInvoked()
        {
            _next.Verify(n => n(It.IsAny<DownstreamContext>()), Times.Never);
        }
    }
}
