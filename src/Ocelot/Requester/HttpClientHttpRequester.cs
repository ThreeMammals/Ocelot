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
        private readonly IOcelotLogger _logger;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(Request.Request request)
        {
            var builder = new HttpClientBuilder();    

            using (var handler = new HttpClientHandler { CookieContainer = request.CookieContainer })
            {
                if (request.IsQos)
                {
                    builder.WithQoS(request.QosProvider, _logger, handler);
                }           

                using (var httpClient = builder.Build(handler))
                {
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
    }
}