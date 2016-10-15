using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Responder;

namespace Ocelot.Library.Infrastructure.Middleware
{
    public class HttpResponderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpResponder _responder;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IErrorsToHttpStatusCodeMapper _codeMapper;

        public HttpResponderMiddleware(RequestDelegate next, 
            IHttpResponder responder,
            IScopedRequestDataRepository scopedRequestDataRepository, 
            IErrorsToHttpStatusCodeMapper codeMapper)
            :base(scopedRequestDataRepository)
        {
            _next = next;
            _responder = responder;
            _scopedRequestDataRepository = scopedRequestDataRepository;
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
                var response = _scopedRequestDataRepository.Get<HttpResponseMessage>("Response");

                await _responder.CreateResponse(context, response.Data);
            }
        }
    }
}