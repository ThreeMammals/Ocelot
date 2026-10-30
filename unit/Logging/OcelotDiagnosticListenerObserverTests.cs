using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Logging;
using System.Diagnostics;

namespace Ocelot.UnitTests.Logging;

public class OcelotDiagnosticListenerObserverTests : UnitTest
{
    private const string MiddlewareStarting = "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting";
    private const string MiddlewareException = "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException";
    private const string MiddlewareFinished = "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished";

    private readonly OcelotDiagnosticListener _listener;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly DefaultHttpContext _httpContext;

    public OcelotDiagnosticListenerObserverTests()
    {
        _httpContext = new DefaultHttpContext();
        var factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        factory.Setup(x => x.CreateLogger<OcelotDiagnosticListener>()).Returns(_logger.Object);
        _listener = new OcelotDiagnosticListener(factory.Object, new ServiceCollection().BuildServiceProvider());
    }

    [Fact]
    public void Should_dispatch_middleware_analysis_events()
    {
        const string name = "name";
        var exception = new Exception("oh no");
        var diagnosticListener = new DiagnosticListener("OcelotTest");
        using var subscription = diagnosticListener.Subscribe(
            new OcelotDiagnosticListenerObserver(_listener),
            OcelotDiagnosticListenerObserver.IsMiddlewareAnalysisEvent);

        diagnosticListener.Write(MiddlewareStarting, new { name, httpContext = _httpContext, instanceId = Guid.NewGuid(), timestamp = 1L });
        diagnosticListener.Write(MiddlewareException, new { name, httpContext = _httpContext, instanceId = Guid.NewGuid(), timestamp = 2L, duration = 1L, exception });
        diagnosticListener.Write(MiddlewareFinished, new { name, httpContext = _httpContext, instanceId = Guid.NewGuid(), timestamp = 3L, duration = 2L });

        _logger.Verify(x => x.LogTrace(It.Is<Func<string>>(c => c.Invoke() == $"MiddlewareStarting: {name}; {_httpContext.Request.Path}")));
        _logger.Verify(x => x.LogTrace(It.Is<Func<string>>(c => c.Invoke() == $"MiddlewareException: {name}; {exception.Message};")));
        _logger.Verify(x => x.LogTrace(It.Is<Func<string>>(c => c.Invoke() == $"MiddlewareFinished: {name}; {_httpContext.Response.StatusCode}")));
    }

    [Fact]
    public void Should_filter_to_middleware_analysis_event_names()
    {
        OcelotDiagnosticListenerObserver.IsMiddlewareAnalysisEvent(MiddlewareStarting).ShouldBeTrue();
        OcelotDiagnosticListenerObserver.IsMiddlewareAnalysisEvent(MiddlewareException).ShouldBeTrue();
        OcelotDiagnosticListenerObserver.IsMiddlewareAnalysisEvent(MiddlewareFinished).ShouldBeTrue();
        OcelotDiagnosticListenerObserver.IsMiddlewareAnalysisEvent("Other.Event").ShouldBeFalse();
    }

    [Fact]
    public void Should_ignore_unknown_and_invalid_events()
    {
        var observer = new OcelotDiagnosticListenerObserver(_listener);
        var diagnosticListener = new DiagnosticListener("OcelotTest");
        using var subscription = diagnosticListener.Subscribe(observer, OcelotDiagnosticListenerObserver.IsMiddlewareAnalysisEvent);

        diagnosticListener.Write(MiddlewareStarting, new { name = "name" });
        diagnosticListener.Write(MiddlewareException, null);
        diagnosticListener.Write(MiddlewareFinished, new { name = 1, httpContext = _httpContext });
        observer.OnNext(new KeyValuePair<string, object>("Other.Event", new { name = "name" }));

        _logger.Verify(x => x.LogTrace(It.IsAny<Func<string>>()), Times.Never);
    }

    [Fact]
    public void Should_ignore_observer_lifecycle_notifications()
    {
        var observer = new OcelotDiagnosticListenerObserver(_listener);

        observer.OnError(new Exception("oh no"));
        observer.OnCompleted();

        _logger.Verify(x => x.LogTrace(It.IsAny<Func<string>>()), Times.Never);
    }

    [Fact]
    public void Should_reject_a_null_listener()
    {
        Assert.Throws<ArgumentNullException>(() => new OcelotDiagnosticListenerObserver(null));
    }
}
