﻿using System;
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
        private readonly IHttpClientCache _cacheHandlers;
        private readonly IOcelotLogger _logger;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory, IHttpClientCache cacheHandlers)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(Request.HttpRequest request)
        {
            var builder = new HttpClientBuilder();

            var cacheKey = GetCacheKey(request, builder);

            var httpClient = GetHttpClient(cacheKey, builder);

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

        private IHttpClient GetHttpClient(string cacheKey, IHttpClientBuilder builder)
        {
            var httpClient = _cacheHandlers.Get(cacheKey);

            if (httpClient == null)
            {
                httpClient = builder.Create();
            }
            return httpClient;
        }

        private string GetCacheKey(Request.HttpRequest request, IHttpClientBuilder builder)
        {
            string baseUrl = $"{request.HttpRequestMessage.RequestUri.Scheme}://{request.HttpRequestMessage.RequestUri.Authority}";

            if (request.IsQos)
            {
                builder.WithQos(request.QosProvider, _logger);
                baseUrl = $"{baseUrl}{request.QosProvider.CircuitBreaker.CircuitBreakerPolicy.PolicyKey}";
            }
           
            return baseUrl;
        }
    }
}