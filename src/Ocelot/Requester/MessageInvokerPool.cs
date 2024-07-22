using Ocelot.Configuration;
using Ocelot.Logging;
using System.Net.Security;

namespace Ocelot.Requester;

public class MessageInvokerPool : IMessageInvokerPool
{
    private readonly ConcurrentDictionary<MessageInvokerCacheKey, Lazy<HttpMessageInvoker>> _handlersPool;
    private readonly IDelegatingHandlerHandlerFactory _handlerFactory;
    private readonly IOcelotLogger _logger;

    public MessageInvokerPool(IDelegatingHandlerHandlerFactory handlerFactory, IOcelotLoggerFactory loggerFactory)
    {
        _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
        _handlersPool = new ConcurrentDictionary<MessageInvokerCacheKey, Lazy<HttpMessageInvoker>>();

        ArgumentNullException.ThrowIfNull(loggerFactory);
        _logger = loggerFactory.CreateLogger<MessageInvokerPool>();
    }

    public HttpMessageInvoker Get(DownstreamRoute downstreamRoute)
    {
        // Since the comparison is based on the downstream route object reference,
        // and the QoS Options properties can't be changed after the route is created,
        // we don't need to use the timeout value as part of the cache key.
        return _handlersPool.GetOrAdd(
            new MessageInvokerCacheKey(downstreamRoute),
            cacheKey => new Lazy<HttpMessageInvoker>(() => CreateMessageInvoker(cacheKey.DownstreamRoute))
        ).Value;
    }

    public void Clear() => _handlersPool.Clear();

    /// <summary>
    /// TODO This should be configurable and available as global config parameter in ocelot.json.
    /// </summary>
    public const int DefaultRequestTimeoutSeconds = 90;
    private int _requestTimeoutSeconds;

    public int RequestTimeoutSeconds
    {
        get => _requestTimeoutSeconds > 0 ? _requestTimeoutSeconds : DefaultRequestTimeoutSeconds;
        set => _requestTimeoutSeconds = value > 0 ? value : DefaultRequestTimeoutSeconds;
    }

    private HttpMessageInvoker CreateMessageInvoker(DownstreamRoute downstreamRoute)
    {
        var baseHandler = CreateHandler(downstreamRoute);
        var handlers = _handlerFactory.Get(downstreamRoute).Data;
        handlers.Reverse();

        foreach (var delegatingHandler in handlers.Select(handler => handler()))
        {
            delegatingHandler.InnerHandler = baseHandler;
            baseHandler = delegatingHandler;
        }

        // Adding timeout handler to the top of the chain.
        // It's standard behavior to throw TimeoutException after the defined timeout (90 seconds by default)
        var timeoutHandler = new TimeoutDelegatingHandler(downstreamRoute.QosOptions.TimeoutValue == 0
            ? TimeSpan.FromSeconds(RequestTimeoutSeconds)
            : TimeSpan.FromMilliseconds(downstreamRoute.QosOptions.TimeoutValue))
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
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
        };

        _logger.LogWarning(() =>
            $"You have ignored all SSL warnings by using DangerousAcceptAnyServerCertificateValidator for this DownstreamRoute, UpstreamPathTemplate: {downstreamRoute.UpstreamPathTemplate}, DownstreamPathTemplate: {downstreamRoute.DownstreamPathTemplate}");

        return handler;
    }

    private readonly struct MessageInvokerCacheKey : IEquatable<MessageInvokerCacheKey>
    {
        public MessageInvokerCacheKey(DownstreamRoute downstreamRoute)
        {
            DownstreamRoute = downstreamRoute;
        }

        public DownstreamRoute DownstreamRoute { get; }

        public override bool Equals(object obj) => obj is MessageInvokerCacheKey key && Equals(key);

        public bool Equals(MessageInvokerCacheKey other) =>
            EqualityComparer<DownstreamRoute>.Default.Equals(DownstreamRoute, other.DownstreamRoute);

        public override int GetHashCode() => DownstreamRoute.GetHashCode();

        public static bool operator ==(MessageInvokerCacheKey left, MessageInvokerCacheKey right) => left.Equals(right);
        public static bool operator !=(MessageInvokerCacheKey left, MessageInvokerCacheKey right) => !(left == right);
    }
}
