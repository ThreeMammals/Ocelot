using Microsoft.AspNetCore.Http;

namespace Ocelot.Logging
{
    public interface ITracer
    {
        void Event(HttpContext httpContext, string @event);

        Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken,
            Action<string> addTraceIdToRepo,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> baseSendAsync);
    }
}
