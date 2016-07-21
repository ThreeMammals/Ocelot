using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.HostUrlRepository;
using Ocelot.Library.Infrastructure.UrlPathMatcher;
using Ocelot.Library.Infrastructure.UrlPathReplacer;
using Ocelot.Library.Infrastructure.UrlPathTemplateRepository;

namespace Ocelot.Library.Middleware
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUrlPathToUrlPathTemplateMatcher _urlMatcher;
        private readonly IUrlPathTemplateMapRepository _urlPathRepository;
        private readonly IHostUrlMapRepository _hostUrlRepository;
        private readonly IUpstreamUrlPathTemplateVariableReplacer _urlReplacer;
        public ProxyMiddleware(RequestDelegate next, 
            IUrlPathToUrlPathTemplateMatcher urlMatcher,
            IUrlPathTemplateMapRepository urlPathRepository,
            IHostUrlMapRepository hostUrlRepository,
            IUpstreamUrlPathTemplateVariableReplacer urlReplacer)
        {
            _next = next;
            _urlMatcher = urlMatcher;
            _urlPathRepository = urlPathRepository;
            _hostUrlRepository = hostUrlRepository;
            _urlReplacer = urlReplacer;
        }

        public async Task Invoke(HttpContext context)
        {
            
            var path = context.Request.Path.ToString();

            var urlPathTemplateMaps = _urlPathRepository.All;

            UrlPathMatch urlPathMatch = null;
            string upstreamPathUrlTemplate = string.Empty;

            foreach (var template in urlPathTemplateMaps.Data)
            {
                urlPathMatch = _urlMatcher.Match(path, template.DownstreamUrlPathTemplate);

                if (urlPathMatch.Match)
                {
                    upstreamPathUrlTemplate = template.UpstreamUrlPathTemplate;
                    break;
                }
            }

            if (!urlPathMatch.Match)
            {
                throw new Exception("BOOOM TING! no match");
            }
            
            var upstreamHostUrl = _hostUrlRepository.GetBaseUrlMap(urlPathMatch.DownstreamUrlPathTemplate);

            var pathUrl = _urlReplacer.ReplaceTemplateVariable(upstreamPathUrlTemplate, urlPathMatch);

            //make a http request to this endpoint...maybe bring in a library

            await _next.Invoke(context);
        }
    }
}