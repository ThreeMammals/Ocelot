using System.Linq;
using System.Net.Http;
using Ocelot.Configuration;

namespace Ocelot.Requester
{
    public class HttpClientBuilder : IHttpClientBuilder
    {
        private readonly IDelegatingHandlerHandlerFactory _factory;

        public HttpClientBuilder(IDelegatingHandlerHandlerFactory house)
        {
            _factory = house;
        }

        public IHttpClient Create(DownstreamReRoute request)
        {
            var httpclientHandler = new HttpClientHandler { AllowAutoRedirect = request.HttpHandlerOptions.AllowAutoRedirect, UseCookies = request.HttpHandlerOptions.UseCookieContainer};
            
            var client = new HttpClient(CreateHttpMessageHandler(httpclientHandler, request));                
            
            return new HttpClientWrapper(client);
        }

        private HttpMessageHandler CreateHttpMessageHandler(HttpMessageHandler httpMessageHandler, DownstreamReRoute request)
        {
            //todo handle error
            var handlers = _factory.Get(request).Data;

            handlers
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
