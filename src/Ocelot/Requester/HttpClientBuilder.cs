using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Requester
{
    public class HttpClientBuilder : IHttpClientBuilder
    {
        private readonly IDelegatingHandlerHandlerFactory _factory;
        private readonly IHttpClientCache _cacheHandlers;
        private readonly IOcelotLogger _logger;
        private string _cacheKey;
        private HttpClient _httpClient;
        private IHttpClient _client;
        private HttpClientHandler _httpclientHandler;

        public HttpClientBuilder(
            IDelegatingHandlerHandlerFactory factory, 
            IHttpClientCache cacheHandlers, 
            IOcelotLogger logger)
        {
            _factory = factory;
            _cacheHandlers = cacheHandlers;
            _logger = logger;
        }

        public IHttpClient Create(DownstreamContext request)
        {
            _cacheKey = GetCacheKey(request);

            var httpClient = _cacheHandlers.Get(_cacheKey);

            if (httpClient != null)
            {
                return httpClient;
            }

            _httpclientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = request.DownstreamReRoute.HttpHandlerOptions.AllowAutoRedirect,
                UseCookies = request.DownstreamReRoute.HttpHandlerOptions.UseCookieContainer,
                CookieContainer = new CookieContainer()
            };

            _httpClient = new HttpClient(CreateHttpMessageHandler(_httpclientHandler, request.DownstreamReRoute));

            _client = new HttpClientWrapper(_httpClient);

            return _client;
        }

        public void Save()
        {
            _cacheHandlers.Set(_cacheKey, _client, TimeSpan.FromHours(24));
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

        private string GetCacheKey(DownstreamContext request)
        {
            var baseUrl = $"{request.DownstreamRequest.RequestUri.Scheme}://{request.DownstreamRequest.RequestUri.Authority}{request.DownstreamRequest.RequestUri.AbsolutePath}";

            return baseUrl;
        }
    }
}
