namespace Ocelot.Library.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Repository;
    using RequestBuilder;
    using Requester;

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