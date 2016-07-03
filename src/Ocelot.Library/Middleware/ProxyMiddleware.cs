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
            await _next.Invoke(context);
        }
    }
}