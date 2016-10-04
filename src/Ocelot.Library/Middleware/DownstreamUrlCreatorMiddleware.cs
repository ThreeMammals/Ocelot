using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.UrlTemplateReplacer;

namespace Ocelot.Library.Middleware
{
    public class DownstreamUrlCreatorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next, 
            IDownstreamUrlTemplateVariableReplacer urlReplacer)
        {
            _next = next;
            _urlReplacer = urlReplacer;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = GetDownstreamRouteFromOwinItems(context);

            var downstreamUrl = _urlReplacer.ReplaceTemplateVariables(downstreamRoute);

            context.Items.Add("DownstreamUrl", downstreamUrl);

            await _next.Invoke(context);
        }

        private DownstreamRoute GetDownstreamRouteFromOwinItems(HttpContext context)
        {
            object obj;
            DownstreamRoute downstreamRoute = null;
            if (context.Items.TryGetValue("DownstreamRoute", out obj))
            {
                downstreamRoute = (DownstreamRoute) obj;
            }
            return downstreamRoute;
        }
    }
}