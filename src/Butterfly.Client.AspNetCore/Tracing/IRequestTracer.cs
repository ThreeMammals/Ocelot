using System;
using Butterfly.OpenTracing;
using Microsoft.AspNetCore.Http;

namespace Butterfly.Client.AspNetCore
{
    public interface IRequestTracer
    {

        ISpan OnBeginRequest(HttpContext httpContext);

        void OnEndRequest(HttpContext httpContext);

        void OnException(HttpContext httpContext, Exception exception, string @event);
    }
}