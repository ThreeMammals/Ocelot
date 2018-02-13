using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Ocelot.Requester
{
    public class DelegatingHandlerHandlerProvider : IDelegatingHandlerHandlerProvider
    {
        private readonly Dictionary<int, Func<DelegatingHandler>> _handlers;

        public DelegatingHandlerHandlerProvider()
        {
            _handlers = new Dictionary<int, Func<DelegatingHandler>>();
        }

        public void Add(Func<DelegatingHandler> handler)
        {
            var key = _handlers.Count == 0 ? 0 : _handlers.Count + 1;
            _handlers[key] = handler;
        }

        public List<Func<DelegatingHandler>> Get()
        {
            return _handlers.Count > 0 ? _handlers.OrderBy(x => x.Key).Select(x => x.Value).ToList() : new List<Func<DelegatingHandler>>();
        }
    }
}
