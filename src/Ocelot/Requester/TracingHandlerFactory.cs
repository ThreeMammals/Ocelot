using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;
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

    public class FakeServiceTracer : IServiceTracer
    {
        public ITracer Tracer { get; }
        public string ServiceName { get; }
        public string Environment { get; }
        public string Identity { get; }
        public ISpan Start(ISpanBuilder spanBuilder)
        {
            throw new System.NotImplementedException();
        }
    }
}
