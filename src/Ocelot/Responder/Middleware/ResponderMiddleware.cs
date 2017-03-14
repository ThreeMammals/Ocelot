using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Responder.Middleware
{
    /// <summary>
    /// Completes and returns the request and request body, if any pipeline errors occured then sets the appropriate HTTP status code instead.
    /// </summary>
    public class ResponderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpResponder _responder;
        private readonly IErrorsToHttpStatusCodeMapper _codeMapper;
        private readonly IOcelotLogger _logger;

        public ResponderMiddleware(RequestDelegate next, 
            IHttpResponder responder,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IErrorsToHttpStatusCodeMapper codeMapper)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _responder = responder;
            _codeMapper = codeMapper;
            _logger = loggerFactory.CreateLogger<ResponderMiddleware>();

        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogTrace($"entered {MiddlwareName}");
            _logger.LogDebug($"invoking next middleware from {MiddlwareName}");

            await _next.Invoke(context);

            _logger.LogDebug($"returned to {MiddlwareName} after next middleware completed");

            if (PipelineError)
            {
                var errors = PipelineErrors;
                _logger.LogDebug($"{errors.Count} pipeline errors found in {MiddlwareName}. Setting error response status code");

                SetErrorResponse(context, errors);
            }
            else
            {
                _logger.LogDebug("no pipeline errors, setting and returning completed response");
                await _responder.SetResponseOnHttpContext(context, HttpResponseMessage);
            }
            _logger.LogTrace($"completed {MiddlwareName}");
        }

        private void SetErrorResponse(HttpContext context, List<Error> errors)
        {
            var statusCode = _codeMapper.Map(errors);

            if (!statusCode.IsError)
            {
                _responder.SetErrorResponseOnContext(context, statusCode.Data);
            }
            else
            {
                _responder.SetErrorResponseOnContext(context, 500);
            }
        }
    }
}