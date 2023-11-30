using Ocelot.Configuration;
using System.Net.Security;

namespace Ocelot.Requester;

public class MessageInvokerPool : IMessageInvokerPool
{
    private readonly ConcurrentDictionary<MessageInvokerCacheKey, HttpMessageInvoker> _handlersPool;
    private readonly IDelegatingHandlerHandlerFactory _handlerFactory;

    public MessageInvokerPool(IDelegatingHandlerHandlerFactory handlerFactory)
    {
        _handlersPool = new ConcurrentDictionary<MessageInvokerCacheKey, HttpMessageInvoker>();
        _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
    }

    public HttpMessageInvoker Get(DownstreamRoute downstreamRoute)
    {
        var timeout = downstreamRoute.QosOptions.TimeoutValue == 0
            ? TimeSpan.FromSeconds(90)
            : TimeSpan.FromMilliseconds(downstreamRoute.QosOptions.TimeoutValue);

        return _handlersPool.GetOrAdd(new MessageInvokerCacheKey(timeout, downstreamRoute),
            CreateMessageInvoker(downstreamRoute, timeout));
    }

    public void Clear() => _handlersPool.Clear();

    private HttpMessageInvoker CreateMessageInvoker(DownstreamRoute downstreamRoute, TimeSpan timeout)
    {
        var baseHandler = CreateHandler(downstreamRoute);
        var handlers = _handlerFactory.Get(downstreamRoute).Data;

        handlers
            .Select(handler => handler)
            .Reverse()
            .ToList()
            .ForEach(handler =>
            {
                var delegatingHandler = handler();
                delegatingHandler.InnerHandler = baseHandler;
                baseHandler = delegatingHandler;
            });

        var timeoutHandler = new TimeoutDelegatingHandler(timeout)
        {
            InnerHandler = baseHandler,
        };

        return new HttpMessageInvoker(timeoutHandler, true);
    }

    private static HttpMessageHandler CreateHandler(DownstreamRoute downstreamRoute)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = downstreamRoute.HttpHandlerOptions.AllowAutoRedirect,
            UseCookies = downstreamRoute.HttpHandlerOptions.UseCookieContainer,
            UseProxy = downstreamRoute.HttpHandlerOptions.UseProxy,
            MaxConnectionsPerServer = downstreamRoute.HttpHandlerOptions.MaxConnectionsPerServer,
            PooledConnectionLifetime = downstreamRoute.HttpHandlerOptions.PooledConnectionLifeTime,
        };

        if (downstreamRoute.HttpHandlerOptions.UseCookieContainer)
        {
            handler.CookieContainer = new CookieContainer();
        }

        if (downstreamRoute.DangerousAcceptAnyServerCertificateValidator)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = delegate { return true; },
            };
        }

        return handler;
    }

    private readonly struct MessageInvokerCacheKey : IEquatable<MessageInvokerCacheKey>
    {
        private TimeSpan Timeout { get; }
        private DownstreamRoute DownstreamRoute { get; }

        public MessageInvokerCacheKey(TimeSpan timeout, DownstreamRoute downstreamRoute)
        {
            Timeout = timeout;
            DownstreamRoute = downstreamRoute;
        }

        public override bool Equals(object obj)
        {
            return obj is MessageInvokerCacheKey key && Equals(key);
        }

        public bool Equals(MessageInvokerCacheKey other)
        {
            var equality = Timeout.Equals(other.Timeout)
                   && EqualityComparer<DownstreamRoute>.Default.Equals(DownstreamRoute, other.DownstreamRoute);
            return equality;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timeout, DownstreamRoute);
        }

        public static bool operator ==(MessageInvokerCacheKey left, MessageInvokerCacheKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MessageInvokerCacheKey left, MessageInvokerCacheKey right)
        {
            return !(left == right);
        }
    }
}
