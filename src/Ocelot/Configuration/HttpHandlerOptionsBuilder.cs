namespace Ocelot.Configuration
{
    public class HttpHandlerOptionsBuilder
    {
        private bool _allowAutoRedirect;
        private bool _useCookieContainer;
        private bool _useTracing;
        private bool _useProxy;

        public HttpHandlerOptionsBuilder WithAllowAutoRedirect(bool input)
        {
            _allowAutoRedirect = input;
            return this;
        }

        public HttpHandlerOptionsBuilder WithUseCookieContainer(bool input)
        {
            _useCookieContainer = input;
            return this;
        }

        public HttpHandlerOptionsBuilder WithUseTracing(bool input)
        {
            _useTracing = input;
            return this;
        }

        public HttpHandlerOptionsBuilder WithUseProxy(bool useProxy)
        {
            _useProxy = useProxy;
            return this;
        }

        public HttpHandlerOptions Build()
        {
            return new HttpHandlerOptions(_allowAutoRedirect, _useCookieContainer, _useTracing, _useProxy);
        }
    }
}
