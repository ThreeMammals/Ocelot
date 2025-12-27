using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Requester;
using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests.Requester;

public class TestMessageInvokerPool : MessageInvokerPool, IMessageInvokerPool
{
    public TestMessageInvokerPool(IDelegatingHandlerFactory handlerFactory, IOcelotLoggerFactory loggerFactory)
        : base(handlerFactory, loggerFactory) { }

    public readonly ConcurrentDictionary<DownstreamRoute, SocketsHttpHandler> CreatedHandlers = new();
    protected override SocketsHttpHandler CreateHandler(DownstreamRoute route)
    {
        var handler = base.CreateHandler(route);
        CreatedHandlers[route] = handler;
        return handler;
    }
}
