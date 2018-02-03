using Butterfly.OpenTracing;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Tracing
{
    public interface IOcelotRequestTracer
    {
        ISpan OnBeginRequest(HttpContext httpContext, Ocelot.Request.Request request);

        void OnEndRequest(HttpContext httpContext, Ocelot.Request.Request request);

        //void OnException(HttpContext httpContext, Exception exception, string @event);
    }
}
