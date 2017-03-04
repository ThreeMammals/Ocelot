using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester
{
    internal class HttpClientBuilder : IHttpClientBuilder
    {
        private TimeSpan? _timeout;
        private readonly List<DelegatingHandler> _handlers = new List<DelegatingHandler>();
        private Dictionary<string, string> _defaultHeaders;
        private CookieContainer _cookieContainer;
        private IQoSProvider _qoSProvider;

        public IHttpClientBuilder WithCookieContainer(CookieContainer cookieContainer)
        {
            _cookieContainer = cookieContainer;
            return this;
        }

        public IHttpClientBuilder WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        public IHttpClientBuilder WithHandler(DelegatingHandler handler)
        {
            _handlers.Add(handler);
            return this;
        }

        public IHttpClientBuilder WithDefaultRequestHeaders(Dictionary<string, string> headers)
        {
            _defaultHeaders = headers;
            return this;
        }


        public IHttpClient Create()
        {
            HttpClientHandler httpclientHandler = null;
            if (_cookieContainer != null)
            {
                httpclientHandler = new HttpClientHandler() { CookieContainer = _cookieContainer };
            }
            else
            {
                httpclientHandler = new HttpClientHandler();
            }

            if (httpclientHandler.SupportsAutomaticDecompression)
            {
                httpclientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            var client = new HttpClient(CreateHttpMessageHandler(httpclientHandler));                

            if (_timeout.HasValue)
            {
                client.Timeout = _timeout.Value;
            }
           
            if (_defaultHeaders == null)
            {
                return new HttpClientWrapper(client);
            }

            foreach (var header in _defaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            return new HttpClientWrapper(client);
        }

        private HttpMessageHandler CreateHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {            
            foreach (var handler in _handlers)
            {
                handler.InnerHandler = httpMessageHandler;
                httpMessageHandler = handler;
            }       
            return httpMessageHandler;
        }
    }

    /// <summary>
    /// This class was made to make unit testing easier when HttpClient is used.
    /// </summary>
    internal class HttpClientWrapper : IHttpClient
    {
        public HttpClient Client { get; }

        public HttpClientWrapper(HttpClient client)
        {
            Client = client;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return Client.SendAsync(request);
        }
    }
}
