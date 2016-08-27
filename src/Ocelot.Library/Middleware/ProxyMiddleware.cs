using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.UrlMatcher;
using Ocelot.Library.Infrastructure.UrlTemplateRepository;
using Ocelot.Library.Infrastructure.UrlTemplateReplacer;

namespace Ocelot.Library.Middleware
{
    using System.Net;

    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUrlPathToUrlTemplateMatcher _urlMatcher;
        private readonly IUrlTemplateMapRepository _urlTemplateMapRepository;
        private readonly IDownstreamUrlTemplateVariableReplacer _urlReplacer;
        public ProxyMiddleware(RequestDelegate next, 
            IUrlPathToUrlTemplateMatcher urlMatcher,
            IUrlTemplateMapRepository urlPathRepository,
            IDownstreamUrlTemplateVariableReplacer urlReplacer)
        {
            _next = next;
            _urlMatcher = urlMatcher;
            _urlTemplateMapRepository = urlPathRepository;
            _urlReplacer = urlReplacer;
        }

        public async Task Invoke(HttpContext context)
        {     
            var downstreamUrlPath = context.Request.Path.ToString();

            var upstreamUrlTemplates = _urlTemplateMapRepository.All;

            UrlMatch urlMatch = null;

            string downstreamUrlTemplate = string.Empty;

            foreach (var template in upstreamUrlTemplates.Data)
            {
                urlMatch = _urlMatcher.Match(downstreamUrlPath, template.DownstreamUrlTemplate);

                if (urlMatch.Match)
                {
                    downstreamUrlTemplate = template.DownstreamUrlTemplate;
                    break;
                }
            }

            if (urlMatch == null || !urlMatch.Match)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }
            
            var downstreamUrl = _urlReplacer.ReplaceTemplateVariable(downstreamUrlTemplate, urlMatch);

            //make a http request to this endpoint...maybe bring in a library

            await _next.Invoke(context);
        }
    }
}