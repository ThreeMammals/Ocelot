using System.Linq;
using System.Net;
using System.Net.Http;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester
{
    public class HttpClientBuilder : IHttpClientBuilder
    {
        private IDelegatingHandlerHandlerProvider _provider;

        public HttpClientBuilder(IDelegatingHandlerHandlerProvider provider)
        {
            _provider = provider;
        }

        public IHttpClientBuilder WithQos(IQoSProvider qosProvider, IOcelotLogger logger)
        {
            _provider.Add(() => new PollyCircuitBreakingDelegatingHandler(qosProvider, logger));
            return this;
        }  

        public IHttpClient Create(bool useCookies, bool allowAutoRedirect)
        {
            var httpclientHandler = new HttpClientHandler { AllowAutoRedirect = allowAutoRedirect, UseCookies = useCookies};
            
            var client = new HttpClient(CreateHttpMessageHandler(httpclientHandler));                
            
            return new HttpClientWrapper(client);
        }

        private HttpMessageHandler CreateHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {            
            _provider.Get()
                .Select(handler => handler)
                .Reverse()
                .ToList()
                .ForEach(handler =>
                {
                    var delegatingHandler = handler();
                    delegatingHandler.InnerHandler = httpMessageHandler;
                    httpMessageHandler = delegatingHandler;
                });
            return httpMessageHandler;
        }
    }
}
