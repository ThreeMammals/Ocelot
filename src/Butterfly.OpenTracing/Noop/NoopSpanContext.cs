using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.OpenTracing.Noop
{
    public class NoopSpanContext : ISpanContext
    {
        public string TraceId => string.Empty;

        public string SpanId => string.Empty;

        public bool Sampled => true;

        public Baggage Baggage { get; } = new Baggage();
        
        public SpanReferenceCollection References { get; } =new SpanReferenceCollection();
    }
}