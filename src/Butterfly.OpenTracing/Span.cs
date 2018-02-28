using System;
using System.Threading;

namespace Butterfly.OpenTracing
{
    internal class Span : ISpan
    {
        private readonly ISpanRecorder _spanRecorder;
        private DateTimeOffset _finishTimestamp;
        private int _state;

        public DateTimeOffset StartTimestamp { get; }

        public DateTimeOffset FinishTimestamp => _finishTimestamp;

        public ISpanContext SpanContext { get; }

        public TagCollection Tags { get; }
        
        public LogCollection Logs { get; }

        public string OperationName { get; }

        public Span(string operationName, DateTimeOffset startTimestamp, ISpanContext spanContext, ISpanRecorder spanRecorder)
        {
            _state = 0;
            _spanRecorder = spanRecorder ?? throw new ArgumentNullException(nameof(spanRecorder));
            SpanContext = spanContext ?? throw new ArgumentNullException(nameof(spanContext));
            Tags = new TagCollection();
            Logs = new LogCollection();
            OperationName = operationName;
            StartTimestamp = startTimestamp;
        }

        public void Dispose()
        {
            Finish(DateTimeOffset.UtcNow);
        }

        public void Finish(DateTimeOffset finishTimestamp)
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 1)
            {
                _finishTimestamp = DateTime.UtcNow;
                _spanRecorder.Record(this);
            }
        }
    }
}