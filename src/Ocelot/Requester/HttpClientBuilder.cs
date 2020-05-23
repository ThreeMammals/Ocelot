namespace Ocelot.Requester
{
    using Ocelot.Configuration;
    using Ocelot.Logging;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    public class HttpClientBuilder : IHttpClientBuilder
    {
        private readonly IDelegatingHandlerHandlerFactory _factory;
        private readonly IHttpClientCache _cacheHandlers;
        private readonly IOcelotLogger _logger;
        private DownstreamReRoute _cacheKey;
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

        public IHttpClient Create(DownstreamReRoute downstreamReRoute)
        {
            _cacheKey = downstreamReRoute;

            var httpClient = _cacheHandlers.Get(_cacheKey);

            if (httpClient != null)
            {
                _client = httpClient;
                return httpClient;
            }

            var handler = CreateHandler(downstreamReRoute);

            if (downstreamReRoute.DangerousAcceptAnyServerCertificateValidator)
            {
                handler.ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => true;

                _logger
                    .LogWarning($"You have ignored all SSL warnings by using DangerousAcceptAnyServerCertificateValidator for this DownstreamReRoute, UpstreamPathTemplate: {downstreamReRoute.UpstreamPathTemplate}, DownstreamPathTemplate: {downstreamReRoute.DownstreamPathTemplate}");
            }

            var timeout = downstreamReRoute.QosOptions.TimeoutValue == 0
                ? _defaultTimeout
                : TimeSpan.FromMilliseconds(downstreamReRoute.QosOptions.TimeoutValue);

            _httpClient = new HttpClient(CreateHttpMessageHandler(handler, downstreamReRoute))
            {
                Timeout = timeout
            };

            _client = new HttpClientWrapper(_httpClient);

            return _client;
        }

        private HttpClientHandler CreateHandler(DownstreamReRoute downstreamReRoute)
        {
            // Dont' create the CookieContainer if UseCookies is not set or the HttpClient will complain
            // under .Net Full Framework
            var useCookies = downstreamReRoute.HttpHandlerOptions.UseCookieContainer;

            return useCookies ? UseCookiesHandler(downstreamReRoute) : UseNonCookiesHandler(downstreamReRoute);
        }

        private HttpClientHandler UseNonCookiesHandler(DownstreamReRoute downstreamReRoute)
        {
            return new HttpClientHandler
            {
                AllowAutoRedirect = downstreamReRoute.HttpHandlerOptions.AllowAutoRedirect,
                UseCookies = downstreamReRoute.HttpHandlerOptions.UseCookieContainer,
                UseProxy = downstreamReRoute.HttpHandlerOptions.UseProxy,
                MaxConnectionsPerServer = downstreamReRoute.HttpHandlerOptions.MaxConnectionsPerServer,
            };
        }

        private HttpClientHandler UseCookiesHandler(DownstreamReRoute downstreamReRoute)
        {
            return new HttpClientHandler
            {
                AllowAutoRedirect = downstreamReRoute.HttpHandlerOptions.AllowAutoRedirect,
                UseCookies = downstreamReRoute.HttpHandlerOptions.UseCookieContainer,
                UseProxy = downstreamReRoute.HttpHandlerOptions.UseProxy,
                MaxConnectionsPerServer = downstreamReRoute.HttpHandlerOptions.MaxConnectionsPerServer,
                CookieContainer = new CookieContainer(),
            };
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
    }
}
