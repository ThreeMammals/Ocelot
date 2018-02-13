using System;
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
        private readonly IHttpClientCache _cacheHandlers;
        private readonly IOcelotLogger _logger;
        private readonly IDelegatingHandlerHandlerHouse _house;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory, 
            IHttpClientCache cacheHandlers,
            IDelegatingHandlerHandlerHouse house)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
            _house = house;
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(Request.Request request)
        {
            var builder = new HttpClientBuilder(_house);

            var cacheKey = GetCacheKey(request);
            
            var httpClient = GetHttpClient(cacheKey, builder, request);

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
            finally
            {
                _cacheHandlers.Set(cacheKey, httpClient, TimeSpan.FromHours(24));
            }

        }

        private IHttpClient GetHttpClient(string cacheKey, IHttpClientBuilder builder, Request.Request request)
        {
            var httpClient = _cacheHandlers.Get(cacheKey);

            if (httpClient == null)
            {
                httpClient = builder.Create(request);
            }

            return httpClient;
        }

        private string GetCacheKey(Request.Request request)
        {
            var baseUrl = $"{request.HttpRequestMessage.RequestUri.Scheme}://{request.HttpRequestMessage.RequestUri.Authority}";

            if (request.IsQos)
            {
                baseUrl = $"{baseUrl}{request.QosProvider.CircuitBreaker.CircuitBreakerPolicy.PolicyKey}";
            }
           
            return baseUrl;
        }
    }
}