using System;

namespace Butterfly.OpenTracing
{
    public class SpanReference
    {
        public SpanReferenceOptions SpanReferenceOptions { get; }

        public ISpanContext SpanContext { get; }

        public SpanReference(SpanReferenceOptions spanReferenceOptions, ISpanContext spanContext)
        {
            SpanReferenceOptions = spanReferenceOptions;
            SpanContext = spanContext ?? throw new ArgumentNullException(nameof(spanContext));
        }
    }
}