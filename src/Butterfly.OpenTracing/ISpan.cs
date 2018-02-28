using System;

namespace Butterfly.OpenTracing
{
    public interface ISpan : IDisposable
    {
        DateTimeOffset StartTimestamp { get; }

        DateTimeOffset FinishTimestamp { get; }

        string OperationName { get; }

        ISpanContext SpanContext { get; }

        TagCollection Tags { get; }

        LogCollection Logs { get; }

        void Finish(DateTimeOffset finishTimestamp);
    }
}