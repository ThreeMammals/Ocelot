using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerProviderFactory
    {
        
    }
    public class DelegatingHandlerHandlerProviderFactory : IDelegatingHandlerHandlerProviderFactory
    {

    }

    public interface IDelegatingHandlerHandlerHouse
    {
        
    }

    public class DelegatingHandlerHandlerHouse : IDelegatingHandlerHandlerHouse
    {

    }

    public class DelegatingHandlerHandlerProvider : IDelegatingHandlerHandlerProvider
    {
        private Dictionary<int, Func<DelegatingHandler>> _handlers;

        public DelegatingHandlerHandlerProvider()
        {
            _handlers = new Dictionary<int, Func<DelegatingHandler>>();
        }

        public void Add(Func<DelegatingHandler> handler)
        {
            var key = _handlers.Count - 1;
            _handlers[key] = handler;
        }

        public List<Func<DelegatingHandler>> Get()
        {
            return _handlers.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        }
    }
}
