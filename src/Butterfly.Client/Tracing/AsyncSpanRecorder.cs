using System;
using Butterfly.OpenTracing;

namespace Butterfly.Client.Tracing
{
    public class AsyncSpanRecorder : ISpanRecorder
    {
        private readonly IButterflyDispatcher _dispatcher;

        public AsyncSpanRecorder(IButterflyDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void Record(ISpan span)
        {
            _dispatcher.Dispatch(SpanContractUtils.CreateFromSpan(span));
        }
    }
}