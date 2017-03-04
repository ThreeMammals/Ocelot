using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Logging;
using Ocelot.Responses;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        private IHttpClientMessageCacheHandler _cacheHandlers;
        private readonly IOcelotLogger _logger;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory, IHttpClientMessageCacheHandler cacheHandlers)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(Request.Request request)
        {
            var builder = new HttpClientBuilder();

            builder.WithCookieContainer(request.CookieContainer);

            string baseUrl = $"{request.HttpRequestMessage.RequestUri.Scheme}://{request.HttpRequestMessage.RequestUri.Authority}";

            if (request.IsQos)
            {
                builder.WithHandler(new PollyCircuitBreakingDelegatingHandler(request.QosProvider, _logger));
                baseUrl = $"{baseUrl}{request.QosProvider.CircuitBreaker.CircuitBreakerPolicy.PolicyKey}";
            }

            IHttpClient httpClient = _cacheHandlers.Get(baseUrl);
            if (httpClient == null)
            {
                httpClient = builder.Create();
                _cacheHandlers.Set(baseUrl, httpClient, TimeSpan.FromMinutes(30));
            } 
   
            try
            {
                var response = await httpClient.SendAsync(request.HttpRequestMessage);
                return new OkResponse<HttpResponseMessage>(response);
            }
            catch (TimeoutRejectedException exception)
            {
                return
                    new ErrorResponse<HttpResponseMessage>(new RequestTimedOutError(exception));
            }
            catch (BrokenCircuitException exception)
            {
                return
                    new ErrorResponse<HttpResponseMessage>(new RequestTimedOutError(exception));
            }
            catch (Exception exception)
            {
                return new ErrorResponse<HttpResponseMessage>(new UnableToCompleteRequestError(exception));
            }

        }
    }
}