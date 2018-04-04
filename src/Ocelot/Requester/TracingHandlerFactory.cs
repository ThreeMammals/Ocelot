using Butterfly.Client.Tracing;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Requester
{
    public class TracingHandlerFactory : ITracingHandlerFactory
    {
        private readonly IServiceTracer _tracer;
        private readonly IRequestScopedDataRepository _repo;

        public TracingHandlerFactory(
            IServiceTracer tracer,
            IRequestScopedDataRepository repo)
        {
            _repo = repo;
            _tracer = tracer;
        }

        public ITracingHandler Get()
        {
            return new OcelotHttpTracingHandler(_tracer, _repo);
        }
    }
}
