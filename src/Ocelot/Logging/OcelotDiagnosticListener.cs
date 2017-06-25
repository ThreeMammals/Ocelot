using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Ocelot.Logging
{
    public class OcelotDiagnosticListener
    {
        private IOcelotLogger _logger;

        public OcelotDiagnosticListener(IOcelotLoggerFactory factory)
        {
            _logger = factory.CreateLogger<OcelotDiagnosticListener>();
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting")]
        public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
        {
            _logger.LogTrace($"MiddlewareStarting: {name}; {httpContext.Request.Path}");
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
        }
    }
}
