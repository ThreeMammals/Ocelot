using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Requester;
using Ocelot.Requester.Middleware;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Requester;

public class HttpRequesterMiddlewareTests : UnitTest
{
    private readonly Mock<IHttpRequester> _requester;
    private Response<HttpResponseMessage> _response;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly HttpRequesterMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly HttpContext _httpContext;

    public HttpRequesterMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _requester = new Mock<IHttpRequester>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<HttpRequesterMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new HttpRequesterMiddleware(_next, _loggerFactory.Object, _requester.Object);
    }

    [Fact]
    public void Should_call_services_correctly()
    {
        GivenTheRequestIs();
        GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.OK)));
        WhenICallTheMiddleware();
        ThenTheDownstreamResponseIsSet();
        InformationIsLogged();
    }

    [Fact]
    public void Should_set_error()
    {
        GivenTheRequestIs();
        GivenTheRequesterReturns(new ErrorResponse<HttpResponseMessage>(new AnyError()));
        WhenICallTheMiddleware();
        ThenTheErrorIsSet();
    }

    [Fact]
    public void Should_log_downstream_internal_server_error()
    {
        GivenTheRequestIs();
        GivenTheRequesterReturns(
            new OkResponse<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        WhenICallTheMiddleware();
        WarningIsLogged();
    }

    [Theory]
    [Trait("Bug", "1953")]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public void Should_LogInformation_when_status_is_less_than_BadRequest(HttpStatusCode status)
    {
        GivenTheRequestIs();
        GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage(status)));
        WhenICallTheMiddleware();
        InformationIsLogged();
    }

    [Theory]
    [Trait("Bug", "1953")]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public void Should_LogWarning_when_status_is_BadRequest_or_greater(HttpStatusCode status)
    {
        GivenTheRequestIs();
        GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage(status)));
        WhenICallTheMiddleware();
        WarningIsLogged();
    }

    private void ThenTheErrorIsSet()
    {
        _httpContext.Items.Errors().Count.ShouldBeGreaterThan(0);
    }

    private Task WhenICallTheMiddleware() => _middleware.Invoke(_httpContext);

    private void GivenTheRequestIs()
    {
        _httpContext.Items.UpsertDownstreamRoute(new DownstreamRouteBuilder().Build());
    }

    private void GivenTheRequesterReturns(Response<HttpResponseMessage> response)
    {
        _response = response;

        _requester
            .Setup(x => x.GetResponse(It.IsAny<HttpContext>()))
            .ReturnsAsync(_response);
    }

    private void ThenTheDownstreamResponseIsSet()
    {
        foreach (var httpResponseHeader in _response.Data.Headers)
        {
            if (_httpContext.Items.DownstreamResponse().Headers.Any(x => x.Key == httpResponseHeader.Key))
            {
                throw new Exception("Header in response not in downstreamresponse headers");
            }
        }

        _httpContext.Items.DownstreamResponse().Content.ShouldBe(_response.Data.Content);
        _httpContext.Items.DownstreamResponse().StatusCode.ShouldBe(_response.Data.StatusCode);
    }

    private void WarningIsLogged()
    {
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Once);
    }

    private void InformationIsLogged()
    {
        _logger.Verify(
            x => x.LogInformation(It.IsAny<Func<string>>()),
            Times.Once);
    }
}
