namespace Ocelot.Configuration
{
    /// <summary>
    /// Describes configuration parameters for http handler,
    /// that is created to handle a request to service
    /// </summary>
    public class HttpHandlerOptions
    {
        public HttpHandlerOptions(bool allowAutoRedirect, bool useCookieContainer, bool useTracing, bool useProxy)
        {
            AllowAutoRedirect = allowAutoRedirect;
            UseCookieContainer = useCookieContainer;
            UseTracing = useTracing;
            UseProxy = useProxy;
        }

        /// <summary>
        /// Specify if auto redirect is enabled
        /// </summary>
        public bool AllowAutoRedirect { get; private set; }

        /// <summary>
        /// Specify is handler has to use a cookie container
        /// </summary>
        public bool UseCookieContainer { get; private set; }

        /// <summary>
        /// Specify is handler has to use a opentracing
        /// </summary>
        public bool UseTracing { get; private set; }

        /// <summary>
        /// Specify if handler has to use a proxy
        /// </summary>
        public bool UseProxy { get; private set; }
    }
}
