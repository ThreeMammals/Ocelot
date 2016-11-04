using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.Responder.Middleware
{
    public class HttpErrorResponderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpResponder _responder;
        private readonly IErrorsToHttpStatusCodeMapper _codeMapper;

        public HttpErrorResponderMiddleware(RequestDelegate next, 
            IHttpResponder responder,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IErrorsToHttpStatusCodeMapper codeMapper)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _responder = responder;
            _codeMapper = codeMapper;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next.Invoke(context);

            if (PipelineError())
            {
                var errors = GetPipelineErrors();

                await SetErrorResponse(context, errors);
            }
            else
            {
                var setResponse = await _responder.SetResponseOnHttpContext(context, HttpResponseMessage);

                if (setResponse.IsError)
                {
                    await SetErrorResponse(context, setResponse.Errors);
                }
            }
        }

        private async Task SetErrorResponse(HttpContext context, List<Error> errors)
        {
            var statusCode = _codeMapper.Map(errors);

            if (!statusCode.IsError)
            {
                await _responder.SetErrorResponseOnContext(context, statusCode.Data);
            }
            else
            {
                await _responder.SetErrorResponseOnContext(context, 500);
            }
        }
    }
}