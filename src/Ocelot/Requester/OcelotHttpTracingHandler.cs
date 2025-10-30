using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.Requester;

public class OcelotHttpTracingHandler : DelegatingHandler, ITracingHandler
{
    private readonly IOcelotTracer _tracer;
    private readonly IRequestScopedDataRepository _repo;

    public OcelotHttpTracingHandler(
        IOcelotTracer tracer,
        IRequestScopedDataRepository repo,
        HttpMessageHandler httpMessageHandler = null)
    {
        _repo = repo;
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        InnerHandler = httpMessageHandler ?? new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _tracer.SendAsync(request,
            x => _repo.Add("TraceId", x),
            base.SendAsync, // implicit anonymous delegate
            cancellationToken);
    }
}
