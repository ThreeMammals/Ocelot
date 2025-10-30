using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

/// <summary>
/// Describes configuration parameters for http handler, that is created to handle a request to service.
/// </summary>
public class HttpHandlerOptions
{
    public const int DefaultPooledConnectionLifetimeSeconds = 120;

    public HttpHandlerOptions() { }

    public HttpHandlerOptions(FileHttpHandlerOptions from)
    {
        AllowAutoRedirect = from.AllowAutoRedirect;
        MaxConnectionsPerServer = (from.MaxConnectionsPerServer > 0) ? from.MaxConnectionsPerServer : int.MaxValue;
        PooledConnectionLifeTime = TimeSpan.FromSeconds(from.PooledConnectionLifetimeSeconds ?? DefaultPooledConnectionLifetimeSeconds);
        UseCookieContainer = from.UseCookieContainer;
        UseProxy = from.UseProxy;
        UseTracing = from.UseTracing;
    }

    public HttpHandlerOptions(bool allowAutoRedirect, bool useCookieContainer, bool useTracing, bool useProxy,
        int maxConnectionsPerServer, TimeSpan pooledConnectionLifeTime)
    {
        AllowAutoRedirect = allowAutoRedirect;
        MaxConnectionsPerServer = maxConnectionsPerServer;
        PooledConnectionLifeTime = pooledConnectionLifeTime;
        UseCookieContainer = useCookieContainer;
        UseProxy = useProxy;
        UseTracing = useTracing;
    }

    /// <summary>
    /// Specify if auto redirect is enabled.
    /// </summary>
    /// <value>AllowAutoRedirect.</value>
    public bool AllowAutoRedirect { get; init; }

    /// <summary>
    /// Specify is handler has to use a cookie container.
    /// </summary>
    /// <value>UseCookieContainer.</value>
    public bool UseCookieContainer { get; init; }

    /// <summary>
    /// Specify is handler has to use a opentracing.
    /// </summary>
    /// <value>UseTracing.</value>
    public bool UseTracing { get; init; }

    /// <summary>
    /// Specify if handler has to use a proxy.
    /// </summary>
    /// <value>UseProxy.</value>
    public bool UseProxy { get; init; }

    /// <summary>
    /// Specify the maximum of concurrent connection to a network endpoint.
    /// </summary>
    /// <value>MaxConnectionsPerServer.</value>
    public int MaxConnectionsPerServer { get; init; }

    /// <summary>
    /// Specify the maximum of time a connection can be pooled.
    /// </summary>
    /// <value>PooledConnectionLifeTime.</value>
    public TimeSpan PooledConnectionLifeTime { get; init; }
}
