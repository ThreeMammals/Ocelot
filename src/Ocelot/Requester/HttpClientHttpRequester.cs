using System;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        private readonly IHttpClientCache _cacheHandlers;
        private readonly IOcelotLogger _logger;
        private readonly IDelegatingHandlerHandlerFactory _factory;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory, 
            IHttpClientCache cacheHandlers,
            IDelegatingHandlerHandlerFactory house)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
            _factory = house;
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(DownstreamContext request)
        {
            var builder = new HttpClientBuilder(_factory);

            var cacheKey = GetCacheKey(request);
            
            var httpClient = GetHttpClient(cacheKey, builder, request);

            try
            {
                var response = await httpClient.SendAsync(request.DownstreamRequest);
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

        private IHttpClient GetHttpClient(string cacheKey, IHttpClientBuilder builder, DownstreamContext request)
        {
            var httpClient = _cacheHandlers.Get(cacheKey);

            if (httpClient == null)
            {
                httpClient = builder.Create(request.DownstreamReRoute);
            }

            return httpClient;
        }

        private string GetCacheKey(DownstreamContext request)
        {
            var baseUrl = $"{request.DownstreamRequest.RequestUri.Scheme}://{request.DownstreamRequest.RequestUri.Authority}";
           
            return baseUrl;
        }
    }
}
