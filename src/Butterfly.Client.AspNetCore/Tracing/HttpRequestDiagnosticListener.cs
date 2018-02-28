using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Butterfly.Client.AspNetCore
{
    public class HttpRequestDiagnosticListener : ITracingDiagnosticListener
    {
        private readonly IRequestTracer _requestTracer;

        public HttpRequestDiagnosticListener(IRequestTracer requestTracer)
        {
            _requestTracer = requestTracer;
        }

        public string ListenerName { get; } = "Microsoft.AspNetCore";

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
        public void HttpRequestIn()
        {
            // do nothing, just enable the diagnotic source
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")]
        public void HttpRequestInStart(HttpContext httpContext)
        {
            _requestTracer.OnBeginRequest(httpContext);
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")]
        public void HttpRequestInStop(HttpContext httpContext)
        {
            _requestTracer.OnEndRequest(httpContext);
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void DiagnosticHandledException(HttpContext httpContext, Exception exception)
        {
            _requestTracer.OnException(httpContext, exception, "AspNetCore HandledException");
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void DiagnosticUnhandledException(HttpContext httpContext, Exception exception)
        {
            _requestTracer.OnException(httpContext, exception, "AspNetCore UnhandledException");
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void HostingUnhandledException(HttpContext httpContext, Exception exception)
        {
            _requestTracer.OnException(httpContext, exception, "AspNetCore UnhandledException");
        }
    }
}