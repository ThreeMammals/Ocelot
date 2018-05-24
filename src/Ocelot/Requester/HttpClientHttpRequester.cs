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
                var message = context.DownstreamRequest.ToHttpRequestMessage();
                /** 
                 * According to https://tools.ietf.org/html/rfc7231
                 * GET,HEAD,DELETE,CONNECT,TRACE
                 * Can have body but server can reject the request.
                 * And MS HttpClient in Full Framework actually rejects it.
                 * see #366 issue 
                **/

                if (message.Method == HttpMethod.Get ||
                    message.Method == HttpMethod.Head ||
                    message.Method == HttpMethod.Delete ||
                    message.Method == HttpMethod.Trace)
                {
                    message.Content = null;
                }
                _logger.LogDebug(string.Format("Sending {0}", message));
                var response = await httpClient.SendAsync(message);
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
