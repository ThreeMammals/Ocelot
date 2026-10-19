using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Logging;

public class OcelotDiagnosticListener
{
    private readonly IOcelotLogger _logger;
    private readonly IOcelotTracer _tracer;

    public OcelotDiagnosticListener(IOcelotLoggerFactory factory, IServiceProvider serviceProvider)
    {
        _logger = factory.CreateLogger<OcelotDiagnosticListener>();
        _tracer = serviceProvider.GetService<IOcelotTracer>();
    }

    public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
    {
        _logger.LogTrace(() => $"MiddlewareStarting: {name}; {httpContext.Request.Path}");
        Event(httpContext, $"MiddlewareStarting: {name}; {httpContext.Request.Path}");
    }

    public virtual void OnMiddlewareException(Exception exception, string name)
    {
        _logger.LogTrace(() => $"MiddlewareException: {name}; {exception.Message};");
    }

    public virtual void OnMiddlewareFinished(HttpContext httpContext, string name)
    {
        _logger.LogTrace(() => $"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
        Event(httpContext, $"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
    }

    protected virtual void Event(HttpContext httpContext, string @event)
    {
        _tracer?.Event(httpContext, @event);
    }
}
