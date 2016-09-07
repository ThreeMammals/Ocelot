using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.UrlTemplateReplacer;

namespace Ocelot.Library.Middleware
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;
        private readonly IOptions<Configuration> _configuration;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;

        public ProxyMiddleware(RequestDelegate next, 
            IDownstreamUrlTemplateVariableReplacer urlReplacer, 
            IOptions<Configuration> configuration, 
            IDownstreamRouteFinder downstreamRouteFinder)
        {
            _next = next;
            _urlReplacer = urlReplacer;
            _configuration = configuration;
            _downstreamRouteFinder = downstreamRouteFinder;
        }

        public async Task Invoke(HttpContext context)
        {   
            var upstreamUrlPath = context.Request.Path.ToString();

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath);

            if (downstreamRoute.IsError)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            var downstreamUrl = _urlReplacer.ReplaceTemplateVariable(downstreamRoute.Data);

            using (var httpClient = new HttpClient())
            {
                var httpMethod = new HttpMethod(context.Request.Method);

                var httpRequestMessage = new HttpRequestMessage(httpMethod, downstreamUrl);

                var response = await httpClient.SendAsync(httpRequestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    context.Response.StatusCode = (int)response.StatusCode;
                    return;
                }
                await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
            }

            await _next.Invoke(context);
        }
    }
}