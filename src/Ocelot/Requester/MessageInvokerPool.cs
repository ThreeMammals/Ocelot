using Microsoft.Extensions.Options;
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
    private readonly FileGlobalConfiguration _globalConfiguration;

    public MessageInvokerPool(
        IDelegatingHandlerFactory handlerFactory,
        IOcelotLoggerFactory loggerFactory,
        IOptions<FileGlobalConfiguration> globalOptions)
    {
        ArgumentNullException.ThrowIfNull(handlerFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(globalOptions);

        _handlersPool = new();
        _handlerFactory = handlerFactory;
        _logger = loggerFactory.CreateLogger<MessageInvokerPool>();
        _globalConfiguration = globalOptions.Value;
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

    private HttpMessageInvoker CreateMessageInvoker(DownstreamRoute route)
    {
        var baseHandler = CreateHandler(route);
        var handlers = _handlerFactory.Get(route);
        handlers.Reverse();
        foreach (var handler in handlers)
        {
            handler.InnerHandler = baseHandler;
            baseHandler = handler;
        }

        int milliseconds = EnsureRouteTimeoutIsGreaterThanQosOne(route);
        var timeout = TimeSpan.FromMilliseconds(milliseconds);

        // Adding timeout handler to the top of the chain.
        // It's standard behavior to throw TimeoutException after the defined timeout (90 seconds by default)
        var timeoutHandler = new TimeoutDelegatingHandler(timeout)
        {
            InnerHandler = baseHandler,
        };
        return new HttpMessageInvoker(timeoutHandler, true);
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
        var routeQos = route.QosOptions;
        if (TryEnsureQosLevel(routeQos, route, false, ref routeMilliseconds))
        {
            return routeMilliseconds;
        }

        var globalQos = new QoSOptions(_globalConfiguration.QoSOptions);
        if (TryEnsureQosLevel(globalQos, route, true, ref routeMilliseconds))
        {
            return routeMilliseconds;
        }

        return routeMilliseconds;
    }

    private bool TryEnsureQosLevel(QoSOptions qos, DownstreamRoute route, bool isGlobal, ref int routeMilliseconds)
    {
        // (qos.UseQos && qos.TimeoutValue.HasValue && routeMilliseconds <= qos.TimeoutValue)
        if (!qos.UseQos || !qos.TimeoutValue.HasValue || routeMilliseconds > qos.TimeoutValue)
        {
            return false;
        }

        int milliseconds = routeMilliseconds;
        int doubledTimeout = 2 * qos.TimeoutValue.Value;
        Func<string> getWarning = route.Timeout.HasValue
            ? () => $"Route '{route.Name()}' has {Global(isGlobal)}Quality of Service settings ({nameof(FileRoute.QoSOptions)}) enabled, but either the route {nameof(route.Timeout)} or the {Global(isGlobal)}QoS {nameof(QoSOptions.TimeoutValue)} is misconfigured: specifically, the route {nameof(route.Timeout)} ({milliseconds} ms) {EqualitySentence(milliseconds, qos.TimeoutValue.Value)} the {Global(isGlobal)}QoS {nameof(QoSOptions.TimeoutValue)} ({qos.TimeoutValue} ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the {Global(isGlobal)}QoS {nameof(QoSOptions.TimeoutValue)} and applied {doubledTimeout} ms to the route {nameof(route.Timeout)}. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!"
            : () => $"Route '{route.Name()}' has {Global(isGlobal)}Quality of Service settings ({nameof(FileRoute.QoSOptions)}) enabled, but either the {nameof(DownstreamRoute)}.{nameof(DownstreamRoute.DefaultTimeoutSeconds)} or the {Global(isGlobal)}QoS {nameof(QoSOptions.TimeoutValue)} is misconfigured: specifically, the {nameof(DownstreamRoute)}.{nameof(DownstreamRoute.DefaultTimeoutSeconds)} ({milliseconds} ms) {EqualitySentence(milliseconds, qos.TimeoutValue.Value)} the {Global(isGlobal)}QoS {nameof(QoSOptions.TimeoutValue)} ({qos.TimeoutValue} ms). To mitigate potential request failures, logged errors, or unexpected behavior caused by Polly's timeout strategy, Ocelot auto-doubled the {Global(isGlobal)}QoS {nameof(QoSOptions.TimeoutValue)} and applied {doubledTimeout} ms to the route {nameof(route.Timeout)} instead of using {nameof(DownstreamRoute)}.{nameof(DownstreamRoute.DefaultTimeoutSeconds)}. However, this adjustment does not guarantee correct Polly behavior. Therefore, it's essential to assign correct values to both timeouts as soon as possible!";
        _logger.LogWarning(getWarning);
        routeMilliseconds = doubledTimeout;
        return true;
    }

    public static string Global(bool isGlobal) => isGlobal ? "global " : string.Empty;

    public static string EqualitySentence(int left, int right)
        => left < right ? "is shorter than" : left == right ? "is equal to" : "is longer than";

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
