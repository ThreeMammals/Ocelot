using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;
using Butterfly.Client.AspNetCore;
using Butterfly.OpenTracing;
using Ocelot.Middleware;

namespace Ocelot.Logging
{
    public class OcelotDiagnosticListener
    {
        private IOcelotLogger _logger;

        public OcelotDiagnosticListener(IOcelotLoggerFactory factory)
        {
            _logger = factory.CreateLogger<OcelotDiagnosticListener>();
        }

        [DiagnosticName("Ocelot.MiddlewareException")]
        public virtual void OcelotMiddlewareException(Exception exception, DownstreamContext context, string name)
        {
            _logger.LogTrace($"Ocelot.MiddlewareException: {name}; {exception.Message}");
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
            _logger.LogTrace($"OcelotMiddlewareFinished: {name}; {context.HttpContext.Request.Path}");
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
            _logger.LogTrace($"MiddlewareException: {name}; {exception.Message}");
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished")]
        public virtual void OnMiddlewareFinished(HttpContext httpContext, string name)
        {
            _logger.LogTrace($"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
            Event(httpContext, $"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
        }

        private void Event(HttpContext httpContext, string @event)
        {
            var span = httpContext.GetSpan();
            span?.Log(LogField.CreateNew().Event(@event));
        }
    }
}
