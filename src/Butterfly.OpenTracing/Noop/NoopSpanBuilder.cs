using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.OpenTracing.Noop
{
    public class NoopSpanBuilder : ISpanBuilder
    {
        public static readonly ISpanBuilder Instance = new NoopSpanBuilder();

        public SpanReferenceCollection References { get; } = new SpanReferenceCollection();

        public string OperationName { get; }

        public DateTimeOffset? StartTimestamp { get; }

        public Baggage Baggage { get; } = new Baggage();

        public bool? Sampled { get; }
    }
}