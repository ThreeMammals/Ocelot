using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Creator;
using Ocelot.Request.Mapper;

namespace Ocelot.Request.Middleware;

public class DownstreamRequestInitialiserMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestMapper _requestMapper;
    private readonly IDownstreamRequestCreator _creator;

    public DownstreamRequestInitialiserMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IRequestMapper requestMapper,
        IDownstreamRequestCreator creator)
        : base(loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>())
    {
        _next = next;
        _requestMapper = requestMapper;
        _creator = creator;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var downstreamRoute = httpContext.Items.DownstreamRoute();
        HttpRequestMessage httpRequestMessage;

        try
        {
            httpRequestMessage = _requestMapper.Map(httpContext.Request, downstreamRoute);
        }
        catch (Exception ex)
        {
            // TODO Review the error handling, we should throw an exception here and use the global error handler middleware to catch it
            httpContext.Items.SetError(new UnmappableRequestError(ex));
            return;
        }

        var downstreamRequest = _creator.Create(httpRequestMessage);
        httpContext.Items.UpsertDownstreamRequest(downstreamRequest);

        await _next.Invoke(httpContext);
    }
}
