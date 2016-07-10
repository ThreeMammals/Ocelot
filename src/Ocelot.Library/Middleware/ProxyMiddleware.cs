using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Library.Middleware
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;

        public ProxyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            //get the downstream host from the request context
            //get the upstream host from the host repository
            //if no upstream host fail this request
            //get the downstream path from the request context
            //get the downstream path template from the path template finder
            //todo think about variables..
            //add any query string..
            await _next.Invoke(context);
        }
    }
}