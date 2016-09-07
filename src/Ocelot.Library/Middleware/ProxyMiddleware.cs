using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.UrlMatcher;
using Ocelot.Library.Infrastructure.UrlTemplateReplacer;

namespace Ocelot.Library.Middleware
{
    using System.Net;
    using Infrastructure.Configuration;
    using Microsoft.Extensions.Options;

    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;
        private readonly IOptions<Configuration> _configuration;

        public ProxyMiddleware(RequestDelegate next, 
            IUrlPathToUrlTemplateMatcher urlMatcher,
            IDownstreamUrlTemplateVariableReplacer urlReplacer, IOptions<Configuration> configuration)
        {
            _next = next;
            _urlMatcher = urlMatcher;
            _urlReplacer = urlReplacer;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {   
            var upstreamUrlPath = context.Request.Path.ToString();

            UrlMatch urlMatch = null;

            foreach (var template in _configuration.Value.ReRoutes)
            {
                urlMatch = _urlMatcher.Match(upstreamUrlPath, template.UpstreamTemplate);

                if (urlMatch.Match)
                {
                    break;
                }
            }

            if (urlMatch == null || !urlMatch.Match)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }
            
            var downstreamUrl = _urlReplacer.ReplaceTemplateVariable(urlMatch);

            //make a http request to this endpoint...maybe bring in a library

            await _next.Invoke(context);
        }
    }
}