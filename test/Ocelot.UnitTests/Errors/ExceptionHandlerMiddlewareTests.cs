using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Errors.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Errors;

public class ExceptionHandlerMiddlewareTests : UnitTest
{
    private bool _shouldThrowAnException;
    private readonly Mock<IRequestScopedDataRepository> _repo;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly ExceptionHandlerMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _httpContext;

    public ExceptionHandlerMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _repo = new Mock<IRequestScopedDataRepository>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ExceptionHandlerMiddleware>()).Returns(_logger.Object);
        _next = async context =>
        {
            await Task.CompletedTask;
            if (_shouldThrowAnException)
            {
                throw new Exception("BOOM");
            }

            _httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
        };
        _middleware = new ExceptionHandlerMiddleware(_next, _loggerFactory.Object, _repo.Object);
    }

    [Fact]
    public async Task NoDownstreamException()
    {
        // Arrange
        _shouldThrowAnException = false;
        var config = new InternalConfiguration();
        _httpContext.Items.Add(nameof(IInternalConfiguration), config);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        _repo.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DownstreamException()
    {
        // Arrange
        _shouldThrowAnException = true;
        var config = new InternalConfiguration();
        _httpContext.Items.Add(nameof(IInternalConfiguration), config);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ShouldSetRequestId()
    {
        // Arrange
        _shouldThrowAnException = false;
        var config = new InternalConfiguration()
        {
            RequestId = "requestidkey",
        };
        _httpContext.Items.Add(nameof(IInternalConfiguration), config);
        _httpContext.Request.Headers.Append("requestidkey", "1234");

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        _repo.Verify(x => x.Add("RequestId", "1234"), Times.Once);
    }

    [Fact]
    public async Task ShouldSetAspDotNetRequestId()
    {
        // Arrange
        _shouldThrowAnException = false;
        var config = new InternalConfiguration();
        _httpContext.Items.Add(nameof(IInternalConfiguration), config);
        _httpContext.Request.Headers.Append("requestidkey", "1234");

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        _repo.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Should_throw_exception_if_config_provider_throws()
    {
        // Arrange
        _shouldThrowAnException = false;

        // this will break when we handle not having the configuratio in the items dictionary
        _httpContext.Items = new Dictionary<object, object>();
        _httpContext.Request.Headers.Append("requestidkey", "1234");

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    private class FakeError : Error
    {
        internal FakeError()
            : base("meh", OcelotErrorCode.CannotAddDataError, 404)
        {
        }
    }
}
