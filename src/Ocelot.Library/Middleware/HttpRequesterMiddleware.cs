using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Requester;
using Ocelot.Library.Infrastructure.Responder;

namespace Ocelot.Library.Middleware
{
    public class HttpRequesterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IHttpResponder _responder;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IHttpResponder responder)
        {
            _next = next;
            _requester = requester;
            _responder = responder;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamUrl = GetDownstreamUrlFromOwinItems(context);

            var response = await _requester
                .GetResponse(context.Request.Method, downstreamUrl, context.Request.Body, 
                context.Request.Headers, context.Request.Cookies, context.Request.Query, context.Request.ContentType);

            await _responder.CreateResponse(context, response);

            await _next.Invoke(context);
        }

        private string GetDownstreamUrlFromOwinItems(HttpContext context)
        {
            object obj;
            string downstreamUrl = null;
            if (context.Items.TryGetValue("DownstreamUrl", out obj))
            {
                downstreamUrl = (string) obj;
            }
            return downstreamUrl;
        }
    }
}