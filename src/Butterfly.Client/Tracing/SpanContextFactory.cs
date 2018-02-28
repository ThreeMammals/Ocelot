using Butterfly.OpenTracing;

namespace Butterfly.Client.Tracing
{
    public class SpanContextFactory : ISpanContextFactory
    {
        private readonly ITraceIdGenerator _traceIdGenerator;

        public SpanContextFactory(ITraceIdGenerator traceIdGenerator)
        {
            _traceIdGenerator = traceIdGenerator;
        }

        public ISpanContext Create(SpanContextPackage spanContextPackage)
        {
            return new SpanContext(
               spanContextPackage.TraceId ?? _traceIdGenerator.Next(),
               spanContextPackage.SpanId ?? RandomUtils.NextLong().ToString(),
               spanContextPackage.Sampled,
               spanContextPackage.Baggage ?? new Baggage(),
               spanContextPackage.References);
        }
    }
}