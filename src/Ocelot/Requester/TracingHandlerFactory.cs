using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;

namespace Ocelot.Requester
{
    public class TracingHandlerFactory : ITracingHandlerFactory
    {
        private readonly IServiceTracer _tracer;

        public TracingHandlerFactory(IServiceTracer tracer)
        {
            _tracer = tracer;
        }

        public ITracingHandler Get()
        {
            return new OcelotHttpTracingHandler(_tracer);
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
