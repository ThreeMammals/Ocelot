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
        private readonly IHttpClientHandlerCache _clientHandlerCache;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory,
            IHttpClientCache cacheHandlers,
            IDelegatingHandlerHandlerFactory house, 
            IHttpClientHandlerCache clientHandlerCache)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
            _factory = house;
            _clientHandlerCache = clientHandlerCache;
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(DownstreamContext context)
        {
            var builder = new HttpClientBuilder(_factory, _cacheHandlers, _logger, _clientHandlerCache);

            var httpClient = builder.Create(context);

            try
            {
                var response = await httpClient.SendAsync(context.DownstreamRequest);
                return new OkResponse<HttpResponseMessage>(response);
            }
            catch (TimeoutRejectedException exception)
            {
                return new ErrorResponse<HttpResponseMessage>(new RequestTimedOutError(exception));
            }
            catch (BrokenCircuitException exception)
            {
                return new ErrorResponse<HttpResponseMessage>(new RequestTimedOutError(exception));
            }
            catch (Exception exception)
            {
                return new ErrorResponse<HttpResponseMessage>(new UnableToCompleteRequestError(exception));
            }
            finally
            {
                builder.Save();
            }
        }      
    }
}
