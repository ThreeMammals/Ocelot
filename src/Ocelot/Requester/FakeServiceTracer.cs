using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;

namespace Ocelot.Requester
{
    public class FakeServiceTracer : IServiceTracer
    {
        public ITracer Tracer { get; }
        public string ServiceName { get; }
        public string Environment { get; }
        public string Identity { get; }
        public ISpan Start(ISpanBuilder spanBuilder)
        {
            return null;
        }
    }
}
