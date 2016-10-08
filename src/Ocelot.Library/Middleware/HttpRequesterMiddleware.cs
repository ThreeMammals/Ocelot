using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Requester;
using Ocelot.Library.Infrastructure.Responder;

namespace Ocelot.Library.Middleware
{
    using Infrastructure.RequestBuilder;

    public class HttpRequesterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IHttpResponder _responder;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IHttpResponder responder,
            IScopedRequestDataRepository scopedRequestDataRepository)
        {
            _next = next;
            _requester = requester;
            _responder = responder;
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = _scopedRequestDataRepository.Get<Request>("Request");

            if (request.IsError)
            {
                await _responder.CreateNotFoundResponse(context);
                return;
            }

            var response = await _requester
                .GetResponse(request.Data);

            _scopedRequestDataRepository.Add("Response", response.Data);
            
            await _next.Invoke(context);
        }
    }
}