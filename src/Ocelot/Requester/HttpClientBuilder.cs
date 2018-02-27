using System.Linq;
using System.Net.Http;
using Ocelot.Configuration;

namespace Ocelot.Requester
{
    public class HttpClientBuilder : IHttpClientBuilder
    {
        private readonly IDelegatingHandlerHandlerHouse _house;

        public HttpClientBuilder(IDelegatingHandlerHandlerHouse house)
        {
            _house = house;
        }

        public IHttpClient Create(DownstreamReRoute request)
        {
            var httpclientHandler = new HttpClientHandler { AllowAutoRedirect = request.HttpHandlerOptions.AllowAutoRedirect, UseCookies = request.HttpHandlerOptions.UseCookieContainer};
            
            var client = new HttpClient(CreateHttpMessageHandler(httpclientHandler, request));                
            
            return new HttpClientWrapper(client);
        }

        private HttpMessageHandler CreateHttpMessageHandler(HttpMessageHandler httpMessageHandler, DownstreamReRoute request)
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
