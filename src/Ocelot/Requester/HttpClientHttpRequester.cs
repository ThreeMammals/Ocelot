using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Requester;

public class HttpClientHttpRequester : IHttpRequester
{
    private readonly IOcelotLogger _logger;
    private readonly IExceptionToErrorMapper _mapper;
    private readonly IMessageInvokerPool _messageHandlerPool;

    public HttpClientHttpRequester(IOcelotLoggerFactory loggerFactory,
        IMessageInvokerPool messageHandlerPool,
        IExceptionToErrorMapper mapper)
    {
        _logger = loggerFactory.CreateLogger<HttpClientHttpRequester>();
        _messageHandlerPool = messageHandlerPool;
        _mapper = mapper;
    }

    public async Task<Response<HttpResponseMessage>> GetResponse(HttpContext httpContext)
    {
        var downstreamRequest = httpContext.Items.DownstreamRequest();
        var messageInvoker = _messageHandlerPool.Get(httpContext.Items.DownstreamRoute());
        try
        {
            var response = await messageInvoker.SendAsync(downstreamRequest.ToHttpRequestMessage(), httpContext.RequestAborted);
            return new OkResponse<HttpResponseMessage>(response);
        }
        catch (Exception exception)
        {
            var error = _mapper.Map(exception);
            return new ErrorResponse<HttpResponseMessage>(error);
        }
    }
}
