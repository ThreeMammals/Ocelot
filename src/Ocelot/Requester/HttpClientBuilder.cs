using System.Linq;
using System.Net.Http;

namespace Ocelot.Requester
{
    public class HttpClientBuilder : IHttpClientBuilder
    {
        private readonly IDelegatingHandlerHandlerHouse _house;

        public HttpClientBuilder(IDelegatingHandlerHandlerHouse house)
        {
            _house = house;
        }

        public IHttpClient Create(bool useCookies, bool allowAutoRedirect, Request.Request request)
        {
            var httpclientHandler = new HttpClientHandler { AllowAutoRedirect = allowAutoRedirect, UseCookies = useCookies};
            
            var client = new HttpClient(CreateHttpMessageHandler(httpclientHandler, request));                
            
            return new HttpClientWrapper(client);
        }

        private HttpMessageHandler CreateHttpMessageHandler(HttpMessageHandler httpMessageHandler, Request.Request request)
        {
            var provider = _house.Get(request);

            //todo handle error
            provider.Data.Get()
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
