using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Logging;

public class OcelotDiagnosticListenerTests : UnitTest
{
    private readonly OcelotDiagnosticListener _listener;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly IServiceCollection _serviceCollection;
    private readonly IServiceProvider _serviceProvider;
    private readonly DefaultHttpContext _httpContext;

    public OcelotDiagnosticListenerTests()
    {
        _httpContext = new DefaultHttpContext();
        _factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _serviceCollection = new ServiceCollection();
        _serviceProvider = _serviceCollection.BuildServiceProvider(true);
        _factory.Setup(x => x.CreateLogger<OcelotDiagnosticListener>()).Returns(_logger.Object);
        _listener = new OcelotDiagnosticListener(_factory.Object, _serviceProvider);
    }

    [Fact]
    public void Should_trace_middleware_started()
    {
        // Arrange
        const string name = "name";

        // Act
        _listener.OnMiddlewareStarting(_httpContext, name);

        // Assert
        ThenTheLogIs($"MiddlewareStarting: {name}; {_httpContext.Request.Path}");
    }

    [Fact]
    public void Should_trace_middleware_finished()
    {
        // Arrange
        const string name = "name";

        // Act
        _listener.OnMiddlewareFinished(_httpContext, name);

        // Assert
        ThenTheLogIs($"MiddlewareFinished: {name}; {_httpContext.Response.StatusCode}");
    }

    [Fact]
    public void Should_trace_middleware_exception()
    {
        // Arrange
        const string name = "name";
        var exception = new Exception("oh no");

        // Act
        _listener.OnMiddlewareException(exception, name);

        // Assert
        ThenTheLogIs($"MiddlewareException: {name}; {exception.Message};");
    }

    private void ThenTheLogIs(string expected)
    {
        _logger.Verify(x => x.LogTrace(It.Is<Func<string>>(c => c.Invoke() == expected)));
    }
}
