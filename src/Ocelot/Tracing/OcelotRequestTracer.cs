using System;
using System.Collections.Generic;
using System.Linq;
using Butterfly.Client.AspNetCore;
using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Tracing
{
    public class OcelotRequestTracer : IOcelotRequestTracer
    {
        private readonly IServiceTracer _tracer;
        private const string prefix_spanId = "ot-spanId";

        public OcelotRequestTracer(IServiceTracer tracer)
        {
            _tracer = tracer;
        }

        public ISpan OnBeginRequest(HttpContext httpContext, Ocelot.Request.Request request)
        {
            var span = httpContext.GetSpan();
            IEnumerable<string> traceIdVals = null;
            if (request.HttpRequestMessage.Headers.TryGetValues(prefix_spanId, out traceIdVals))
            {
                request.HttpRequestMessage.Headers.Remove(prefix_spanId);
                request.HttpRequestMessage.Headers.TryAddWithoutValidation(prefix_spanId, span.SpanContext.SpanId);
            };
            span.Log(LogField.CreateNew().Event("Ocelot.Responder.Middleware.HttpRequesterMiddlewareStarting"));
            _tracer.Tracer.SetCurrentSpan(span);
            return span;
        }

        public void OnEndRequest(HttpContext httpContext, Ocelot.Request.Request request)
        {
            var span = httpContext.GetSpan();
            if (span == null)
            {
                return;
            }

            span.Tags
                .Server().Component("Ocelot")
                .HttpMethod(httpContext.Request.Method)
                .HttpUrl($"{request.HttpRequestMessage.RequestUri}{httpContext.Request.QueryString}")
                .HttpHost(httpContext.Request.Host.ToUriComponent())
                .HttpPath(request.HttpRequestMessage.RequestUri.ToString())
                .HttpStatusCode(httpContext.Response.StatusCode)
                .PeerAddress(httpContext.Connection.RemoteIpAddress.ToString())
                .PeerPort(httpContext.Connection.RemotePort);
            span.Log(LogField.CreateNew().Event("Ocelot.Responder.Middleware.HttpRequesterMiddlewareEnd"));
        }

        //public void OnException(HttpContext httpContext, Exception exception, string @event)
        //{
        //    var span = httpContext.GetSpan();
        //    if (span == null)
        //    {
        //        return;
        //    }
        //    span?.Log(LogField.CreateNew().Event(@event));
        //    span?.Exception(exception);
        //}
    }
}
