using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.OpenTracing.Noop
{
    public class NoopSpan : ISpan
    {
        public ISpanContext SpanContext { get; } = new NoopSpanContext();

        public Baggage Baggage => SpanContext.Baggage;

        public TagCollection Tags { get; } = new TagCollection();

        public LogCollection Logs { get; } = new LogCollection();

        public void Finish(DateTimeOffset finishTimestamp)
        {
        }

        public DateTimeOffset StartTimestamp { get; set; }

        public DateTimeOffset FinishTimestamp { get; set; }

        public string OperationName => string.Empty;

        public void Dispose()
        {
            Finish(DateTimeOffset.UtcNow);
        }
    }
}
