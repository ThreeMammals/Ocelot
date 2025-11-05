using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using System.Net.Security;

namespace Ocelot.Requester;

public class MessageInvokerPool : IMessageInvokerPool
{
    private readonly ConcurrentDictionary<MessageInvokerCacheKey, Lazy<HttpMessageInvoker>> _handlersPool;
    private readonly IDelegatingHandlerFactory _handlerFactory;
    private readonly IOcelotLogger _logger;

    public MessageInvokerPool(
        IDelegatingHandlerFactory handlerFactory,
        IOcelotLoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(handlerFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _handlersPool = new();
        _handlerFactory = handlerFactory;
        _logger = loggerFactory.CreateLogger<MessageInvokerPool>();
    }

    public HttpMessageInvoker Get(DownstreamRoute downstreamRoute)
    {
        // Since the comparison is based on the downstream route object reference,
        // and the QoS Options properties can't be changed after the route is created,
        // we don't need to use the timeout value as part of the cache key.
        return _handlersPool.GetOrAdd(
            new MessageInvokerCacheKey(downstreamRoute),
            cacheKey => new Lazy<HttpMessageInvoker>(() => CreateMessageInvoker(cacheKey.Route))
        ).Value;
    }

    public void Clear() => _handlersPool.Clear();

    protected HttpMessageInvoker CreateMessageInvoker(DownstreamRoute route)
    {
        HttpMessageHandler baseHandler = CreateHandler(route);
        List<DelegatingHandler> handlers = _handlerFactory.Get(route);
        handlers.Reverse();
        foreach (DelegatingHandler handler in handlers)
        {
            handler.InnerHandler = baseHandler;
            baseHandler = handler;
        }

        int milliseconds = EnsureRouteTimeoutIsGreaterThanQosOne(route);
        TimeSpan timeout = TimeSpan.FromMilliseconds(milliseconds);

        // Adding timeout handler to the top of the chain.
        // It's standard behavior to throw TimeoutException after the defined timeout (90 seconds by default)
        HttpMessageHandler timeoutHandler = new TimeoutDelegatingHandler(timeout)
        {
            InnerHandler = baseHandler,
        };
        return new(timeoutHandler, true);
    }

    /// <summary>
    /// Ensures that the route timeout is greater than the QoS timeout. If the route timeout is less than or equal to the QoS timeout, returns double the QoS timeout value and logs a warning.
    /// </summary>
    /// <remarks>The method is open for overriding because it is declared as <see langword="virtual"/>.</remarks>
    /// <param name="route">Current processing route.</param>
    /// <returns>An <see cref="int"/> value representing the timeout in milliseconds, to be assigned in the upper context.</returns>
    protected virtual int EnsureRouteTimeoutIsGreaterThanQosOne(DownstreamRoute route)
    {
        var qos = route.QosOptions;
        int routeMilliseconds = 1_000 * (route.Timeout ?? DownstreamRoute.DefaultTimeoutSeconds);
        if (!qos.UseQos || !qos.TimeoutValue.HasValue || routeMilliseconds > qos.TimeoutValue)
        {
            return routeMilliseconds;
        }

        int milliseconds = routeMilliseconds;
        int doubledTimeout = 2 * qos.TimeoutValue.Value;
        Func<string> getWarning = route.Timeout.HasValue
            ? () => $"Route '{route.Name()}' has Quality of Service settings ({nameof(FileRoute.QoSOptions)}) enabled, but either the route {nameof(route.Timeout)} or the QoS {nameof(QoSOptions.TimeoutValue)} is misconfigured: specifically, the route {nameof(route.Timeout)} ({milliseconds} ms) {EqualitySentence(milliseconds, qos.TimeoutValue.Value)} the QoS {nameof(QoSOptions.TimeoutValue)} ({qos.TimeoutValue} ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the QoS {nameof(QoSOptions.TimeoutValue)} and applied {doubledTimeout} ms to the route {nameof(route.Timeout)}. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!"
            : () => $"Route '{route.Name()}' has Quality of Service settings ({nameof(FileRoute.QoSOptions)}) enabled, but either the {nameof(DownstreamRoute)}.{nameof(DownstreamRoute.DefaultTimeoutSeconds)} or the QoS {nameof(QoSOptions.TimeoutValue)} is misconfigured: specifically, the {nameof(DownstreamRoute)}.{nameof(DownstreamRoute.DefaultTimeoutSeconds)} ({milliseconds} ms) {EqualitySentence(milliseconds, qos.TimeoutValue.Value)} the QoS {nameof(QoSOptions.TimeoutValue)} ({qos.TimeoutValue} ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the QoS {nameof(QoSOptions.TimeoutValue)} and applied {doubledTimeout} ms to the route {nameof(route.Timeout)} instead of using {nameof(DownstreamRoute)}.{nameof(DownstreamRoute.DefaultTimeoutSeconds)}. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!";
        _logger.LogWarning(getWarning);
        return doubledTimeout;
    }

    public static string EqualitySentence(int left, int right)
        => left < right ? "is shorter than" : left == right ? "is equal to" : "is longer than";

    protected SocketsHttpHandler CreateHandler(DownstreamRoute route)
    {
        var options = route.HttpHandlerOptions;
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = options.AllowAutoRedirect,
            UseCookies = options.UseCookieContainer,
            UseProxy = options.UseProxy,
            MaxConnectionsPerServer = options.MaxConnectionsPerServer,
            PooledConnectionLifetime = options.PooledConnectionLifeTime,
        };

        if (options.UseCookieContainer)
        {
            handler.CookieContainer = new CookieContainer();
        }

        if (!route.DangerousAcceptAnyServerCertificateValidator)
        {
            return handler;
        }

        handler.SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
        };
        _logger.LogWarning(() =>
            $"You have ignored all SSL warnings by using {nameof(DownstreamRoute.DangerousAcceptAnyServerCertificateValidator)} for this {nameof(DownstreamRoute)} -> {route.Name()}");
        return handler;
    }

    public readonly struct MessageInvokerCacheKey : IEquatable<MessageInvokerCacheKey>
    {
        public MessageInvokerCacheKey(DownstreamRoute route) => Route = route;

        public DownstreamRoute Route { get; }

        public override bool Equals(object obj) => obj is MessageInvokerCacheKey key && Equals(key);

        public bool Equals(MessageInvokerCacheKey other) =>
            EqualityComparer<DownstreamRoute>.Default.Equals(Route, other.Route);

        public override int GetHashCode() => Route.GetHashCode();

        public static bool operator ==(MessageInvokerCacheKey left, MessageInvokerCacheKey right) => left.Equals(right);
        public static bool operator !=(MessageInvokerCacheKey left, MessageInvokerCacheKey right) => !(left == right);
    }
}
