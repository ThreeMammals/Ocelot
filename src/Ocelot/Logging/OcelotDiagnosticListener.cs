using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DiagnosticAdapter;
using Ocelot.Middleware;
using System;

namespace Ocelot.Logging
{
    public class OcelotDiagnosticListener
    {
        private readonly IOcelotLogger _logger;
        private readonly ITracer _tracer;

        public OcelotDiagnosticListener(IOcelotLoggerFactory factory, IServiceProvider serviceProvider)
        {
            _logger = factory.CreateLogger<OcelotDiagnosticListener>();
            _tracer = serviceProvider.GetService<ITracer>();
        }

        [DiagnosticName("Ocelot.MiddlewareException")]
        public virtual void OcelotMiddlewareException(Exception exception, DownstreamContext context, string name)
        {
            _logger.LogTrace($"Ocelot.MiddlewareException: {name}; {exception.Message};");
            Event(context.HttpContext, $"Ocelot.MiddlewareStarted: {name}; {context.HttpContext.Request.Path}");
        }

        [DiagnosticName("Ocelot.MiddlewareStarted")]
        public virtual void OcelotMiddlewareStarted(DownstreamContext context, string name)
        {
            _logger.LogTrace($"Ocelot.MiddlewareStarted: {name}; {context.HttpContext.Request.Path}");
            Event(context.HttpContext, $"Ocelot.MiddlewareStarted: {name}; {context.HttpContext.Request.Path}");
        }

        [DiagnosticName("Ocelot.MiddlewareFinished")]
        public virtual void OcelotMiddlewareFinished(DownstreamContext context, string name)
        {
            _logger.LogTrace($"Ocelot.MiddlewareFinished: {name}; {context.HttpContext.Request.Path}");
            Event(context.HttpContext, $"OcelotMiddlewareFinished: {name}; {context.HttpContext.Request.Path}");
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting")]
        public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
        {
            _logger.LogTrace($"MiddlewareStarting: {name}; {httpContext.Request.Path}");
            Event(httpContext, $"MiddlewareStarting: {name}; {httpContext.Request.Path}");
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException")]
        public virtual void OnMiddlewareException(Exception exception, string name)
        {
            _logger.LogTrace($"MiddlewareException: {name}; {exception.Message};");
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished")]
        public virtual void OnMiddlewareFinished(HttpContext httpContext, string name)
        {
            _logger.LogTrace($"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
            Event(httpContext, $"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
        }

        private void Event(HttpContext httpContext, string @event)
        {
            _tracer?.Event(httpContext, @event);
        }
    }
}
