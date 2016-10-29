using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
}