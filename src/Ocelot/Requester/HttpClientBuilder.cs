using Microsoft.Extensions.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Requester.QoS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    internal class HttpClientBuilder : IHttpClientBuilder
    {
        private readonly Dictionary<int, Func<DelegatingHandler>> _handlers = new Dictionary<int, Func<DelegatingHandler>>();

        public  IHttpClientBuilder WithQos(IQoSProvider qosProvider, IOcelotLogger logger)
        {
            _handlers.Add(5000, () => new PollyCircuitBreakingDelegatingHandler(qosProvider, logger));

            return this;
        }

        private IHttpClientBuilder WithTracing(IServiceProvider provider)
        {
            _handlers.Add(6000, () => provider.GetService<OcelotHttpTracingHandler>());
            return this;
        }

        public IHttpClient Create(bool useCookies, bool allowAutoRedirect, bool isTracing, IServiceProvider provider)
        {
            var httpclientHandler = new HttpClientHandler { AllowAutoRedirect = allowAutoRedirect, UseCookies = useCookies };
            if (isTracing)
            {
                WithTracing(provider);
            }
            var client = new HttpClient(CreateHttpMessageHandler(httpclientHandler));                
            
            return new HttpClientWrapper(client);
        }

        private HttpMessageHandler CreateHttpMessageHandler(HttpMessageHandler httpMessageHandler)
        {            
            _handlers
                .OrderByDescending(handler => handler.Key)
                .Select(handler => handler.Value)
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
