using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

/// <summary>
/// Describes configuration parameters for http handler, that is created to handle a request to service.
/// </summary>
public class HttpHandlerOptions //: SocketsHttpHandler // TODO Think about using inheritance or composition design since we initialize the SocketsHttpHandler instance with the options
{
    public const int DefaultPooledConnectionLifetimeSeconds = 120;

    public HttpHandlerOptions()
    {
        MaxConnectionsPerServer = int.MaxValue;
        PooledConnectionLifeTime = TimeSpan.FromSeconds(DefaultPooledConnectionLifetimeSeconds);
    }

    public HttpHandlerOptions(FileHttpHandlerOptions from)
    {
        AllowAutoRedirect = from.AllowAutoRedirect ?? false;
        MaxConnectionsPerServer = from.MaxConnectionsPerServer.HasValue && from.MaxConnectionsPerServer.Value > 0
            ? from.MaxConnectionsPerServer.Value : int.MaxValue;
        PooledConnectionLifeTime = TimeSpan.FromSeconds(from.PooledConnectionLifetimeSeconds ?? DefaultPooledConnectionLifetimeSeconds);
        UseCookieContainer = from.UseCookieContainer ?? false;
        UseProxy = from.UseProxy ?? false;
        UseTracing = from.UseTracing ?? false;
        UseDefaultCredentials = from.UseDefaultCredentials ?? false;
    }

    public HttpHandlerOptions(FileHttpHandlerOptions from, bool useTracing)
        : this(from)
    {
        UseTracing = useTracing && (from.UseTracing ?? false);
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
    /// <value>
    /// The maximum number of concurrent connections (per server endpoint) allowed by an <see cref="HttpClient"/> object.
    /// The property value is assignable to the <see cref="HttpClientHandler.MaxConnectionsPerServer"/> one.
    /// </value>
    public int MaxConnectionsPerServer { get; init; }

    /// <summary>
    /// Specify the maximum of time a connection can be pooled.
    /// </summary>
    /// <value>PooledConnectionLifeTime.</value>
    public TimeSpan PooledConnectionLifeTime { get; init; }

    /// <summary>
    /// Specify is UseDefaultCredentials set on HttpClientHandler.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the default credentials are used; otherwise <see langword="false"/>. The default value is <see langword="false"/>.
    /// The property value is assignable to the <see cref="HttpClientHandler.UseDefaultCredentials"/> one.
    /// </value>
    public bool UseDefaultCredentials { get; init; }
}
