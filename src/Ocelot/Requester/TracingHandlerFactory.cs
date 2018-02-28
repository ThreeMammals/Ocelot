using Butterfly.Client.Tracing;

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
}