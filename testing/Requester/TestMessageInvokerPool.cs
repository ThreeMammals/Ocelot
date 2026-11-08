using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Requester;
using System.Collections.Concurrent;

namespace Ocelot.Testing.Requester;

public class TestMessageInvokerPool : MessageInvokerPool
{
    public TestMessageInvokerPool(IDelegatingHandlerFactory handlerFactory, IOcelotLoggerFactory loggerFactory)
        : base(handlerFactory, loggerFactory) { }

    public volatile int Counter;
    public override HttpMessageInvoker Get(DownstreamRoute downstreamRoute)
    {
        var invoker = base.Get(downstreamRoute);
        _ = Interlocked.Increment(ref Counter);
        return invoker;
    }

    public readonly ConcurrentDictionary<DownstreamRoute, SocketsHttpHandler> CreatedHandlers = [];
    public readonly ConcurrentBag<SocketsHttpHandler> AllHandlers = [];
    protected override SocketsHttpHandler CreateHandler(DownstreamRoute route)
    {
        var handler = base.CreateHandler(route);
        CreatedHandlers[route] = handler;
        AllHandlers.Add(handler);
        return handler;
    }
}
