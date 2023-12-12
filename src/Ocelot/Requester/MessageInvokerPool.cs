using Ocelot.Configuration;
using System.Net.Security;
using Ocelot.Logging;

namespace Ocelot.Requester;

public class MessageInvokerPool : IMessageInvokerPool
{
    //todo: this should be configurable and available as global config parameter in ocelot.json
    public const int DefaultRequestTimeoutSeconds = 90;

    private readonly ConcurrentDictionary<MessageInvokerCacheKey, HttpMessageInvoker> _handlersPool;
    private readonly IDelegatingHandlerHandlerFactory _handlerFactory;
    private readonly IOcelotLogger _logger;

    public MessageInvokerPool(IDelegatingHandlerHandlerFactory handlerFactory, IOcelotLogger logger)
    {
        _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlersPool = new ConcurrentDictionary<MessageInvokerCacheKey, HttpMessageInvoker>();
    }

    public HttpMessageInvoker Get(DownstreamRoute downstreamRoute)
    {
        var timeout = downstreamRoute.QosOptions.TimeoutValue == 0
            ? TimeSpan.FromSeconds(DefaultRequestTimeoutSeconds)
            : TimeSpan.FromMilliseconds(downstreamRoute.QosOptions.TimeoutValue);

        // Since the comparison is based on the downstream route object reference,
        // and the QoS Options properties can't be changed after the route is created,
        // we don't need to use the timeout value as part of the cache key.
        return _handlersPool.GetOrAdd(new MessageInvokerCacheKey(downstreamRoute),
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

    private HttpMessageHandler CreateHandler(DownstreamRoute downstreamRoute)
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

        if (!downstreamRoute.DangerousAcceptAnyServerCertificateValidator)
        {
            return handler;
        }

        handler.SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = delegate { return true; },
        };

        _logger.LogWarning(() =>
            $"You have ignored all SSL warnings by using DangerousAcceptAnyServerCertificateValidator for this DownstreamRoute, UpstreamPathTemplate: {downstreamRoute.UpstreamPathTemplate}, DownstreamPathTemplate: {downstreamRoute.DownstreamPathTemplate}");

        return handler;
    }

    private readonly struct MessageInvokerCacheKey : IEquatable<MessageInvokerCacheKey>
    {
        private DownstreamRoute DownstreamRoute { get; }

        public MessageInvokerCacheKey(DownstreamRoute downstreamRoute)
        {
            DownstreamRoute = downstreamRoute;
        }

        public override bool Equals(object obj) => obj is MessageInvokerCacheKey key && Equals(key);

        public bool Equals(MessageInvokerCacheKey other) =>
            EqualityComparer<DownstreamRoute>.Default.Equals(DownstreamRoute, other.DownstreamRoute);

        public override int GetHashCode() => DownstreamRoute.GetHashCode();

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
