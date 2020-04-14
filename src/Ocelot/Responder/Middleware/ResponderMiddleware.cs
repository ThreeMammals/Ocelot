namespace Ocelot.Responder.Middleware
{
    using Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Errors;
    using Ocelot.Infrastructure.Extensions;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
            IErrorsToHttpStatusCodeMapper codeMapper,
            IRequestScopedDataRepository repo
           )
            : base(loggerFactory.CreateLogger<ResponderMiddleware>(), repo)
        {
            _next = next;
            _responder = responder;
            _codeMapper = codeMapper;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            await _next.Invoke(httpContext);

            // todo check errors is ok
            if (Errors.Data.Count > 0)
            {
                Logger.LogWarning($"{Errors.Data.ToErrorString()} errors found in {MiddlewareName}. Setting error response for request path:{httpContext.Request.Path}, request method: {httpContext.Request.Method}");

                SetErrorResponse(httpContext, Errors.Data);
            }
            else
            {
                Logger.LogDebug("no pipeline errors, setting and returning completed response");
                await _responder.SetResponseOnHttpContext(httpContext, DownstreamContext.Data.DownstreamResponse);
            }
        }

        private void SetErrorResponse(HttpContext context, List<Error> errors)
        {
            var statusCode = _codeMapper.Map(errors);
            _responder.SetErrorResponseOnContext(context, statusCode);
        }
    }
}
