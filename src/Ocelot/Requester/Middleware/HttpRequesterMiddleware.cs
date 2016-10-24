using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.RequestBuilder;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requester = requester;
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = _requestScopedDataRepository.Get<Request>("Request");

            if (request.IsError)
            {
                SetPipelineError(request.Errors);
                return;
            }

            var response = await _requester.GetResponse(request.Data);

            if (response.IsError)
            {
                SetPipelineError(response.Errors);
                return;
            }

            _requestScopedDataRepository.Add("Response", response.Data);            
        }
    }
}