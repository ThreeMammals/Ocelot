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
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly HttpRequesterMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public HttpRequesterMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _requester = new Mock<IHttpRequester>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<HttpRequesterMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new HttpRequesterMiddleware(_next, _loggerFactory.Object, _requester.Object);

        _httpContext.Items.UpsertDownstreamRoute(new DownstreamRouteBuilder().Build()); // Given The Request Is
    }

    [Fact]
    public async Task Should_call_services_correctly()
    {
        // Arrange
        var response = GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        InformationIsLogged();

        // Assert: Then The Downstream Response Is Set
        foreach (var httpResponseHeader in response.Data.Headers)
        {
            if (_httpContext.Items.DownstreamResponse().Headers.Any(x => x.Key == httpResponseHeader.Key))
            {
                throw new Exception("Header in response not in downstreamresponse headers");
            }
        }

        _httpContext.Items.DownstreamResponse().Content.ShouldBe(response.Data.Content);
        _httpContext.Items.DownstreamResponse().StatusCode.ShouldBe(response.Data.StatusCode);
    }

    [Fact]
    public async Task Should_set_error()
    {
        // Arrange
        GivenTheRequesterReturns(new ErrorResponse<HttpResponseMessage>(new AnyError()));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.Errors().Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_log_downstream_internal_server_error()
    {
        // Arrange
        GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        WarningIsLogged();
    }

    [Theory]
    [Trait("Bug", "1953")]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public async Task Should_LogInformation_when_status_is_less_than_BadRequest(HttpStatusCode status)
    {
        // Arrange
        GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage(status)));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        InformationIsLogged();
    }

    [Theory]
    [Trait("Bug", "1953")]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Should_LogWarning_when_status_is_BadRequest_or_greater(HttpStatusCode status)
    {
        // Arrange
        GivenTheRequesterReturns(new OkResponse<HttpResponseMessage>(new HttpResponseMessage(status)));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        WarningIsLogged();
    }

    private Response<HttpResponseMessage> GivenTheRequesterReturns(Response<HttpResponseMessage> response)
    {
        _requester.Setup(x => x.GetResponse(It.IsAny<HttpContext>()))
            .ReturnsAsync(response);
        return response;
    }

    private void WarningIsLogged()
    {
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.Once);
    }

    private void InformationIsLogged()
    {
        _logger.Verify(x => x.LogInformation(It.IsAny<Func<string>>()),
            Times.Once);
    }
}
