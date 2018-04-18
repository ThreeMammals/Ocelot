using System;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using Pivotal.Discovery.Client;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Requester
{
    public class DiscoveryClientHttpRequester : IHttpRequester
    {
        private readonly IHttpClientCache _cacheHandlers;
        private readonly IOcelotLogger _logger;
        private readonly IDelegatingHandlerHandlerFactory _factory;
        private readonly IDiscoveryClient _discoveryClient;
        private DiscoveryHttpClientHandler _handler;

        public DiscoveryClientHttpRequester(IOcelotLoggerFactory loggerFactory,
            IHttpClientCache cacheHandlers,
            IDelegatingHandlerHandlerFactory house, IDiscoveryClient discoveryClient)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
            _factory = house;
            _discoveryClient = discoveryClient;
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(DownstreamContext request)
        {
            var builder = new HttpClientBuilder(_factory, _cacheHandlers, _logger);

            _handler = new DiscoveryHttpClientHandler(_discoveryClient);
            var discoveryClientBuilder = new DiscoveryHttpClientBuilder().Create(_handler, request.DownstreamReRoute);
            var httpDiscoveryClient = discoveryClientBuilder.Client;

            try
            {
                var response = await httpDiscoveryClient.SendAsync(request.DownstreamRequest.ToHttpRequestMessage());
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
                builder.Save();
            }
        }
    }
}
