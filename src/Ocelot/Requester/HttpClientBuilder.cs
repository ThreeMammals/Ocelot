using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerProvider
    {
        void Add(Func<DelegatingHandler> handler);
        List<Func<DelegatingHandler>> Get();
    }

    public class DelegatingHandlerHandlerProvider : IDelegatingHandlerHandlerProvider
    {
        private Dictionary<int, Func<DelegatingHandler>> _handlers;

        public DelegatingHandlerHandlerProvider()
        {
            _handlers = new Dictionary<int, Func<DelegatingHandler>>();
        }

        public void Add(Func<DelegatingHandler> handler)
        {
            var key = _handlers.Count - 1;
            _handlers[key] = handler;
        }

        public List<Func<DelegatingHandler>> Get()
        {
            return _handlers.OrderByDescending(x => x.Key).Select(x => x.Value).ToList();
        }
    }


    public class HttpClientBuilder : IHttpClientBuilder
    {
        private IDelegatingHandlerHandlerProvider _provider;

        public HttpClientBuilder(IDelegatingHandlerHandlerProvider provider)
        {
            _provider = provider;
        }

        public  IHttpClientBuilder WithQos(IQoSProvider qosProvider, IOcelotLogger logger)
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
