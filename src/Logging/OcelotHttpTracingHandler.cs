using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Logging;

public class OcelotHttpTracingHandler : DelegatingHandler, ITracingHandler
{
    public const string TraceId = nameof(TraceId);

    private readonly IOcelotTracer _tracer;
    private readonly IRequestScopedDataRepository _repo;

    public OcelotHttpTracingHandler(
        IOcelotTracer tracer,
        IRequestScopedDataRepository repo,
        HttpMessageHandler httpMessageHandler = null)
    {
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        InnerHandler = httpMessageHandler ?? new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _tracer.SendAsync(request, AddTraceId, base.SendAsync, cancellationToken); // TODO This is absolutely wrong

    protected virtual void AddTraceId(string id)
        => _repo.Add(TraceId, id);
}
