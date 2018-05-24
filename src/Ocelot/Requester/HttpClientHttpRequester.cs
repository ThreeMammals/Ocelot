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

        public async Task<Response<HttpResponseMessage>> GetResponse(DownstreamContext context)
        {
            var builder = new HttpClientBuilder(_factory, _cacheHandlers, _logger);

            var httpClient = builder.Create(context);

            try
            {
                var response = await httpClient.SendAsync(context.DownstreamRequest.ToHttpRequestMessage());
                return new OkResponse<HttpResponseMessage>(response);
            }
            catch (TimeoutRejectedException exception)
            {
                return new ErrorResponse<HttpResponseMessage>(new RequestTimedOutError(exception));
            }
            catch (TaskCanceledException exception)
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
