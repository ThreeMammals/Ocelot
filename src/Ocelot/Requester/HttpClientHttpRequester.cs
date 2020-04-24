namespace Ocelot.Requester
{
    using Ocelot.Configuration;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Responses;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class HttpClientHttpRequester : IHttpRequester
    {
        private readonly IHttpClientCache _cacheHandlers;
        private readonly IOcelotLogger _logger;
        private readonly IDelegatingHandlerHandlerFactory _factory;
        private readonly IExceptionToErrorMapper _mapper;

        public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory,
            IHttpClientCache cacheHandlers,
            IDelegatingHandlerHandlerFactory factory,
            IExceptionToErrorMapper mapper)
        {
            _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
            _cacheHandlers = cacheHandlers;
            _factory = factory;
            _mapper = mapper;
        }

        public async Task<Response<HttpResponseMessage>> GetResponse(IDownstreamContext context, HttpContext httpContext, DownstreamReRoute downstreamReRoute)
        {
            var builder = new HttpClientBuilder(_factory, _cacheHandlers, _logger);

            var httpClient = builder.Create(downstreamReRoute);

            try
            {
                var response = await httpClient.SendAsync(context.DownstreamRequest.ToHttpRequestMessage(), httpContext.RequestAborted);
                return new OkResponse<HttpResponseMessage>(response);
            }
            catch (Exception exception)
            {
                var error = _mapper.Map(exception);
                return new ErrorResponse<HttpResponseMessage>(error);
            }
            finally
            {
                builder.Save();
            }
        }
    }
}
