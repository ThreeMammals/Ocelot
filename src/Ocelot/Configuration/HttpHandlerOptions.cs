namespace Ocelot.Configuration
{
    /// <summary>
    /// Describes configuration parameters for http handler, that is created to handle a request to service.
    /// </summary>
    public class HttpHandlerOptions
    {
        public HttpHandlerOptions(bool allowAutoRedirect, bool useCookieContainer, bool useTracing, bool useProxy,
            int maxConnectionsPerServer, TimeSpan pooledConnectionLifeTime)
        {
            AllowAutoRedirect = allowAutoRedirect;
            UseCookieContainer = useCookieContainer;
            UseTracing = useTracing;
            UseProxy = useProxy;
            MaxConnectionsPerServer = maxConnectionsPerServer;
            PooledConnectionLifeTime = pooledConnectionLifeTime;
        }

        /// <summary>
        /// Specify if auto redirect is enabled.
        /// </summary>
        /// <value>AllowAutoRedirect.</value>
        public bool AllowAutoRedirect { get; }

        /// <summary>
        /// Specify is handler has to use a cookie container.
        /// </summary>
        /// <value>UseCookieContainer.</value>
        public bool UseCookieContainer { get; }

        /// <summary>
        /// Specify is handler has to use a opentracing.
        /// </summary>
        /// <value>UseTracing.</value>
        public bool UseTracing { get; }

        /// <summary>
        /// Specify if handler has to use a proxy.
        /// </summary>
        /// <value>UseProxy.</value>
        public bool UseProxy { get; }

        /// <summary>
        /// Specify the maximum of concurrent connection to a network endpoint.
        /// </summary>
        /// <value>MaxConnectionsPerServer.</value>
        public int MaxConnectionsPerServer { get; }

        /// <summary>
        /// Specify the maximum of time a connection can be pooled.
        /// </summary>
        /// <value>PooledConnectionLifeTime.</value>
        public TimeSpan PooledConnectionLifeTime { get; }
    }
}
