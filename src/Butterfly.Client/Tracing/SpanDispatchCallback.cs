using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Butterfly.Client.Logging;
using Butterfly.DataContract.Tracing;

namespace Butterfly.Client
{
    public class SpanDispatchCallback : IDispatchCallback
    {
        private const int DefaultChunked = 500;
        private readonly IButterflySender _butterflySender;
        private readonly Func<DispatchableToken, bool> _filter;
        private readonly ILogger _logger;

        public SpanDispatchCallback(IButterflySenderProvider senderProvider, ILoggerFactory loggerFactory)
        {
            _butterflySender = senderProvider.GetSender();
            _logger = loggerFactory.CreateLogger(typeof(SpanDispatchCallback));
            _filter = token => token == DispatchableToken.SpanToken;
        }

        public Func<DispatchableToken, bool> Filter => _filter;

        public async Task Accept(IEnumerable<IDispatchable> dispatchables)
        {
            foreach(var block in dispatchables.Chunked(DefaultChunked))
            {
                try
                {
                    await _butterflySender.SendSpanAsync(block.Select(x => x.RawInstance).OfType<Span>().ToArray());
                }
                catch(Exception exception)
                {
                    foreach(var item in block)
                    {
                        item.State = SendState.Untreated;
                        item.Error();
                    }
                    _logger.Error("Flush span to collector error.", exception);
                }
            }
        }
    }
}
