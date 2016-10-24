using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.Responder.Middleware
{
    public class HttpResponderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpResponder _responder;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;
        private readonly IErrorsToHttpStatusCodeMapper _codeMapper;

        public HttpResponderMiddleware(RequestDelegate next, 
            IHttpResponder responder,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IErrorsToHttpStatusCodeMapper codeMapper)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _responder = responder;
            _requestScopedDataRepository = requestScopedDataRepository;
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
                    await _responder.CreateErrorResponse(context, statusCode.Data);
                }
                else
                {
                    await _responder.CreateErrorResponse(context, 500);
                }
            }
            else
            {
                var response = _requestScopedDataRepository.Get<HttpResponseMessage>("Response");

                await _responder.CreateResponse(context, response.Data);
            }
        }
    }
}