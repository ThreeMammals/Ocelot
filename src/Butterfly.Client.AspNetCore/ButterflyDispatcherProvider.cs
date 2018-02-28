using System.Collections.Generic;
using Butterfly.Client.Logging;
using Microsoft.Extensions.Options;

namespace Butterfly.Client.AspNetCore
{
    public class ButterflyDispatcherProvider : IButterflyDispatcherProvider
    {
        private readonly IEnumerable<IDispatchCallback> _dispatchCallbacks;
        private readonly ButterflyOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public ButterflyDispatcherProvider(IEnumerable<IDispatchCallback> dispatchCallbacks, ILoggerFactory loggerFactory, IOptions<ButterflyOptions> options)
        {
            _dispatchCallbacks = dispatchCallbacks;
            _loggerFactory = loggerFactory;
            _options = options.Value;
        }

        public IButterflyDispatcher GetDispatcher()
        {
            return new ButterflyDispatcher(_dispatchCallbacks, _loggerFactory, _options.FlushInterval, _options.BoundedCapacity, _options.ConsumerCount);
        }
    }
}