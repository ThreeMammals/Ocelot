using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Errors;
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
    private readonly HttpContext _httpContext;

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
        GivenTheHttpResponseMessageIs(new DownstreamResponse(new HttpResponseMessage()));
        await _middleware.Invoke(_httpContext);
        ThenThereAreNoErrors();
    }

    [Fact]
    public async Task Should_return_any_errors()
    {
        GivenTheHttpResponseMessageIs(new DownstreamResponse(new HttpResponseMessage()));
        GivenThereArePipelineErrors(new UnableToFindDownstreamRouteError("/path", "GET"));
        await _middleware.Invoke(_httpContext);
        ThenThereAreNoErrors();
    }

    [Fact]
    public async Task Should_not_call_responder_when_null_downstream_response()
    {
        this._responder.Reset();
        GivenTheHttpResponseMessageIs(null);
        await _middleware.Invoke(_httpContext);
        ThenThereAreNoErrors();
        _responder.VerifyNoOtherCalls();
    }

    private void GivenTheHttpResponseMessageIs(DownstreamResponse response)
    {
        _httpContext.Items.UpsertDownstreamResponse(response);
    }

    private void ThenThereAreNoErrors()
    {
        //todo a better assert?
    }

    private void GivenThereArePipelineErrors(Error error)
    {
        _httpContext.Items.SetError(error);
    }
}
