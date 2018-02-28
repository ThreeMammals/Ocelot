using System;
using Butterfly.OpenTracing;

namespace Butterfly.Client.Tracing
{
    public class ServiceSpan : ISpan
    {
        private readonly ISpan _span;
        private readonly ISpan _parent;
        private readonly ITracer _tracer;

        public DateTimeOffset StartTimestamp => _span.StartTimestamp;

        public DateTimeOffset FinishTimestamp => _span.FinishTimestamp;

        public string OperationName => _span.OperationName;

        public ISpanContext SpanContext => _span.SpanContext;

        public TagCollection Tags => _span.Tags;

        public LogCollection Logs => _span.Logs;

        public ServiceSpan(ISpan span, ITracer tracer)
        {
            _span = span;
            _tracer = tracer;
            _parent = _tracer.GetCurrentSpan();
            _tracer.SetCurrentSpan(span);
        }

        public void Dispose()
        {
            Finish(DateTimeOffset.UtcNow);
        }

        public void Finish(DateTimeOffset finishTimestamp)
        {
            _span.Finish(finishTimestamp);
            _tracer.SetCurrentSpan(_parent);
        }
    }
}