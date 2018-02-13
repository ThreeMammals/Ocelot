using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerProvider
    {
        void Add(Func<DelegatingHandler> handler);
        List<Func<DelegatingHandler>> Get();
    }
}
