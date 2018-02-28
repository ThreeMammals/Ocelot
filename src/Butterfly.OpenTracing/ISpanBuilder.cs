using System;

namespace Butterfly.OpenTracing
{
    public interface ISpanBuilder
    {
        SpanReferenceCollection References { get; }

        string OperationName { get; }

        DateTimeOffset? StartTimestamp { get; }

        Baggage Baggage { get; }

        bool? Sampled { get; }
    }
}