using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Butterfly.Client.Logging;

namespace Butterfly.Client
{
    internal class TimerDispatchHandler : IDisposable
    {
        private readonly int _flushInterval;
        private readonly Timer _timer;
        private readonly IEnumerable<IDispatchCallback> _callbacks;
        private readonly ConcurrentDictionary<IDispatchable, object> _state;
        private readonly ILogger _logger;

        public TimerDispatchHandler(IEnumerable<IDispatchCallback> callbacks, ILoggerFactory loggerFactory, int flushInterval)
        {
            _callbacks = callbacks;
            _flushInterval = flushInterval;
            _logger = loggerFactory.CreateLogger(typeof(TimerDispatchHandler));
            _state = new ConcurrentDictionary<IDispatchable, object>();
            _timer = new Timer(async s => await FlushCallback(DateTimeOffset.UtcNow), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(_flushInterval));
        }

        private async Task FlushCallback(DateTimeOffset utcNow)
        {
            var oldStates = _state.Where(x => x.Key.Timestamp < utcNow && x.Key.State == SendState.Untreated).Select(x=>x.Key).ToList();
            oldStates.ForEach(item => item.State = SendState.Sending);
            var tokendStates = oldStates.GroupBy(x => x.Token);
            foreach (var tokendState in tokendStates)
            {
                var states = tokendState.Select(x => x).ToList();
                try
                {
                    foreach (var callback in _callbacks)
                    {
                        if (callback.Filter(tokendState.Key))
                        {
                            await callback.Accept(states);
                        }
                    }
                    foreach (var item in states)
                    {
                        if (item.State == SendState.Sending)
                            item.State = SendState.Sended;
                    }
                }
                catch (Exception exception)
                {
                    foreach (var item in states.Where(x => x.State == SendState.Sending))
                    {
                        item.Error();
                        item.State = SendState.Untreated;
                    }
                    _logger.Error("Flush data to collector error.", exception);
                }
                finally
                {
                    foreach (var state in states)
                    {
                        if (state.State == SendState.Sended || state.ErrorCount >= 2)
                            _state.TryRemove(state, out _);
                    }
                }
            }
        }

        public bool Post(IDispatchable val)
        {
            return _state.TryAdd(val, null);
        }

        public void Dispose()
        {
            _timer.Dispose();
            _state.Clear();
        }
    }
}