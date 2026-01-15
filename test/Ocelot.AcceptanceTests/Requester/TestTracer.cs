using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.Requester;

public class TestTracer : IOcelotTracer
{
    public readonly ConcurrentBag<string> Events = new();
    public readonly ConcurrentDictionary<HttpRequestMessage, HttpResponseMessage> Requests = new();

    public void Event(HttpContext httpContext, string @event)
        => Events.Add(@event);

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Action<string> addTraceIdToRepo, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> baseSendAsync, CancellationToken cancellationToken)
    {
        addTraceIdToRepo?.Invoke("12345");
        var response = await baseSendAsync.Invoke(request, cancellationToken).ConfigureAwait(false);
        Requests[request] = response;
        return response;
    }
}
