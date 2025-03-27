using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Responder.Middleware;

/// <summary>
/// Completes and returns the request and request body, if any pipeline errors occured then sets the appropriate HTTP status code instead.
/// </summary>
public class ResponderMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpResponder _responder;
    private readonly IErrorsToHttpStatusCodeMapper _codeMapper;

    public ResponderMiddleware(RequestDelegate next,
        IHttpResponder responder,
        IOcelotLoggerFactory loggerFactory,
        IErrorsToHttpStatusCodeMapper codeMapper)
        : base(loggerFactory.CreateLogger<ResponderMiddleware>())
    {
        _next = next;
        _responder = responder;
        _codeMapper = codeMapper;
    }

    public async Task Invoke(HttpContext context)
    {
        await _next.Invoke(context);

        var errors = context.Items.Errors();
        if (errors.Count > 0)
        {
            Logger.LogWarning(() => $"{MiddlewareName} found {errors.Count} error{errors.Count.Plural()} ->{errors.ToErrorString(true, true)}Setting error response for request: {context.Request.Method} {context.Request.Path}");
            await SetErrorResponse(context, errors);
            return;
        }

        // We are going to dispose the http request message and content in
        // this middleware (no further use). That's why we are using the 'using' statement.
        using var response = context.Items.DownstreamResponse();
        if (response == null)
        {
            Logger.LogDebug(() => $"Pipeline was terminated early in {MiddlewareName}");
            return;
        }

        Logger.LogDebug("No pipeline errors: setting and returning completed response...");
        await _responder.SetResponseOnHttpContext(context, response);
    }

    private async Task SetErrorResponse(HttpContext context, List<Error> errors)
    {
        // TODO The exception/error handling should be reviewed and refactored.
        var statusCode = _codeMapper.Map(errors);
        _responder.SetErrorResponseOnContext(context, statusCode);

        if (errors.All(e => e.Code != OcelotErrorCode.QuotaExceededError))
        {
            return;
        }

        var downstreamResponse = context.Items.DownstreamResponse();
        await _responder.SetErrorResponseOnContext(context, downstreamResponse);
    }
}
