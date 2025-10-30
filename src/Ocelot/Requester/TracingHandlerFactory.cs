using Microsoft.Extensions.DependencyInjection;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.Requester;

public class TracingHandlerFactory : ITracingHandlerFactory
{
    private readonly IOcelotTracer _tracer;
    private readonly IRequestScopedDataRepository _repo;

    public TracingHandlerFactory(
        IServiceProvider services,
        IRequestScopedDataRepository repo)
    {
        _repo = repo;
        _tracer = services.GetService<IOcelotTracer>();
    }

    public ITracingHandler Get()
    {
        return new OcelotHttpTracingHandler(_tracer, _repo);
    }
}
