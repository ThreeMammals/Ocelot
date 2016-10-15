using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.RequestBuilder;
using Ocelot.Library.Infrastructure.Requester;

namespace Ocelot.Library.Infrastructure.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IScopedRequestDataRepository scopedRequestDataRepository)
            :base(scopedRequestDataRepository)
        {
            _next = next;
            _requester = requester;
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = _scopedRequestDataRepository.Get<Request>("Request");

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

            _scopedRequestDataRepository.Add("Response", response.Data);            
        }
    }
}