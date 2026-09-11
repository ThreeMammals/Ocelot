using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responder;
using Ocelot.Responder.Middleware;

namespace Ocelot.UnitTests.Responder;

public class ResponderMiddlewareTests : UnitTest
{
    private readonly Mock<IHttpResponder> _responder;
    private readonly Mock<IErrorsToHttpStatusCodeMapper> _codeMapper;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly ResponderMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public ResponderMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _responder = new Mock<IHttpResponder>();
        _codeMapper = new Mock<IErrorsToHttpStatusCodeMapper>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ResponderMiddleware>()).Returns(_logger.Object);
        _next = context => Task.CompletedTask;
        _middleware = new ResponderMiddleware(_next, _responder.Object, _loggerFactory.Object, _codeMapper.Object);
    }

    [Fact]
    public async Task Should_not_return_any_errors()
    {
        // Arrange
        _httpContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new HttpResponseMessage()));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.Errors().ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_return_any_errors()
    {
        // Arrange
        _httpContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new HttpResponseMessage()));
        _httpContext.Items.SetError(new UnableToFindDownstreamRouteError("/path", "GET"));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.Errors().Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_not_call_responder_when_null_downstream_response()
    {
        // Arrange
        this._responder.Reset();
        _httpContext.Items.UpsertDownstreamResponse(null);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Items.Errors().ShouldBeEmpty();
        _responder.VerifyNoOtherCalls();
    }
}
