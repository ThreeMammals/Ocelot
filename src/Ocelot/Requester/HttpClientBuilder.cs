using Ocelot.Logging;
using Ocelot.Values;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    internal class HttpClientBuilder
    {
        private readonly Dictionary<int, Func<DelegatingHandler>> handlers = new Dictionary<int, Func<DelegatingHandler>>();

        public HttpClientBuilder WithCircuitBreaker(QoS qos, IOcelotLogger logger, HttpMessageHandler innerHandler)
        {
            handlers.Add(5000, () => new CircuitBreakingDelegatingHandler(qos.ExceptionsAllowedBeforeBreaking, qos.DurationOfBreak, qos.TimeoutValue, qos.TimeoutStrategy, logger, innerHandler));
            return this;
        }

        internal HttpClient Build()
        {
            return handlers.Any() ? new HttpClient(CreateHttpMessageHandler()) : new HttpClient();
        }

        private HttpMessageHandler CreateHttpMessageHandler()
        {
            HttpMessageHandler httpMessageHandler = new HttpClientHandler();

            handlers.OrderByDescending(handler => handler.Key).Select(handler => handler.Value).Reverse().ToList().ForEach(handler =>
            {
                var delegatingHandler = handler();
                delegatingHandler.InnerHandler = httpMessageHandler;
                httpMessageHandler = delegatingHandler;
            });

            return httpMessageHandler;
        }
    }
}
