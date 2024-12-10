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
    private string _name;
    private Exception _exception;
    private readonly HttpContext _httpContext;

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
        GivenAMiddlewareName();
        WhenMiddlewareStartedCalled();
        ThenTheLogIs($"MiddlewareStarting: {_name}; {_httpContext.Request.Path}");
    }

    [Fact]
    public void Should_trace_middleware_finished()
    {
        GivenAMiddlewareName();
        WhenMiddlewareFinishedCalled();
        ThenTheLogIs($"MiddlewareFinished: {_name}; {_httpContext.Response.StatusCode}");
    }

    [Fact]
    public void Should_trace_middleware_exception()
    {
        GivenAMiddlewareName();
        GivenAException(new Exception("oh no"));
        WhenMiddlewareExceptionCalled();
        ThenTheLogIs($"MiddlewareException: {_name}; {_exception.Message};");
    }

    private void GivenAException(Exception exception)
    {
        _exception = exception;
    }

    private void WhenMiddlewareStartedCalled()
    {
        _listener.OnMiddlewareStarting(_httpContext, _name);
    }

    private void WhenMiddlewareFinishedCalled()
    {
        _listener.OnMiddlewareFinished(_httpContext, _name);
    }

    private void WhenMiddlewareExceptionCalled()
    {
        _listener.OnMiddlewareException(_exception, _name);
    }

    private void GivenAMiddlewareName()
    {
        _name = "name";
    }

    private void ThenTheLogIs(string expected)
    {
        _logger.Verify(
            x => x.LogTrace(It.Is<Func<string>>(c => c.Invoke() == expected)));
    }
}
