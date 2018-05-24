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
        private readonly TimeSpan _defaultTimeout;

        public HttpClientBuilder(
            IDelegatingHandlerHandlerFactory factory, 
            IHttpClientCache cacheHandlers, 
            IOcelotLogger logger)
        {
            _factory = factory;
            _cacheHandlers = cacheHandlers;
            _logger = logger;

            // This is hardcoded at the moment but can easily be added to configuration
            // if required by a user request.
            _defaultTimeout = TimeSpan.FromSeconds(90);
        }

        public IHttpClient Create(DownstreamContext context)
        {
            _cacheKey = GetCacheKey(context);

            var httpClient = _cacheHandlers.Get(_cacheKey);

            if (httpClient != null)
            {
                return httpClient;
            }
            bool useCookies = context.DownstreamReRoute.HttpHandlerOptions.UseCookieContainer;
            HttpClientHandler httpclientHandler;
            // Dont' create the CookieContainer if UseCookies is not set ot the HttpClient will complain
            // under .Net Full Framework
            if (useCookies)
            {
                httpclientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = context.DownstreamReRoute.HttpHandlerOptions.AllowAutoRedirect,
                    UseCookies = context.DownstreamReRoute.HttpHandlerOptions.UseCookieContainer,
                    CookieContainer = new CookieContainer()
                };
            }
            else
            {
                httpclientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = context.DownstreamReRoute.HttpHandlerOptions.AllowAutoRedirect,
                    UseCookies = context.DownstreamReRoute.HttpHandlerOptions.UseCookieContainer,
                };
            }

            if (context.DownstreamReRoute.DangerousAcceptAnyServerCertificateValidator)
            {
                httpclientHandler.ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => true;

                _logger
                    .LogWarning($"You have ignored all SSL warnings by using DangerousAcceptAnyServerCertificateValidator for this DownstreamReRoute, UpstreamPathTemplate: {context.DownstreamReRoute.UpstreamPathTemplate}, DownstreamPathTemplate: {context.DownstreamReRoute.DownstreamPathTemplate}");
            }

            var timeout = context.DownstreamReRoute.QosOptions.TimeoutValue == 0
                ? _defaultTimeout 
                : TimeSpan.FromMilliseconds(context.DownstreamReRoute.QosOptions.TimeoutValue);

            _httpClient = new HttpClient(CreateHttpMessageHandler(httpclientHandler, context.DownstreamReRoute))
            {
                Timeout = timeout
            };

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
            var cacheKey = $"{request.DownstreamRequest.Method}:{request.DownstreamRequest.OriginalString}";

            this._logger.LogDebug($"Cache key for request is {cacheKey}");

            return cacheKey;
        }
    }
}
