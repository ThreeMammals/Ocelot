using Butterfly.Client.AspNetCore;
using Butterfly.OpenTracing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;
using System;

namespace Ocelot.Tracing
{
    public class OcelotTracingDiagnosticListener : ITracingDiagnosticListener
    {
        public string ListenerName { get; } = "Ocelot.Tracing";

        private readonly IOcelotRequestTracer _requestTracer;

        public OcelotTracingDiagnosticListener(IOcelotRequestTracer requestTracer)
        {
            _requestTracer = requestTracer;
        }

        [DiagnosticName("Ocelot.Responder.Middleware.ResponderMiddlewareStarting")]
        public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
        {
            Event(httpContext, "Ocelot.Responder.Middleware.ResponderMiddlewareStarting");
        }

        [DiagnosticName("Ocelot.Responder.Middleware.HttpRequesterMiddlewareStarting")]
        public void HttpRequesterMiddlewareStarting(HttpContext httpContext, Request.Request request)
        {
            _requestTracer.OnBeginRequest(httpContext, request);
        }

        [DiagnosticName("Ocelot.Responder.Middleware.HttpRequesterMiddlewareEnd")]
        public void HttpRequesterMiddlewareEnd(HttpContext httpContext, Request.Request request)
        {
            _requestTracer.OnEndRequest(httpContext, request);
        }

        private void Event(HttpContext httpContext, string @event)
        {
            var span = httpContext.GetSpan();
            span?.Log(LogField.CreateNew().Event(@event));
        }
    }
}
