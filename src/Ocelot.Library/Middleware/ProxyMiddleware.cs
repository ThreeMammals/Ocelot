using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.HostUrlRepository;
using Ocelot.Library.Infrastructure.UrlPathMatcher;
using Ocelot.Library.Infrastructure.UrlPathTemplateRepository;

namespace Ocelot.Library.Middleware
{
    public class ProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IUrlPathToUrlPathTemplateMatcher _urlMatcher;
        private readonly IUrlPathTemplateMapRepository _urlPathRepository;
        private readonly IHostUrlMapRepository _hostUrlRepository;
        public ProxyMiddleware(RequestDelegate next, 
            IUrlPathToUrlPathTemplateMatcher urlMatcher,
            IUrlPathTemplateMapRepository urlPathRepository,
            IHostUrlMapRepository hostUrlRepository)
        {
            _next = next;
            _urlMatcher = urlMatcher;
            _urlPathRepository = urlPathRepository;
            _hostUrlRepository = hostUrlRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            
            var path = context.Request.Path.ToString();

            var templates = _urlPathRepository.All;

            UrlPathMatch urlPathMatch = null;
            string upstreamPathUrl = string.Empty;

            foreach (var template in templates.Data)
            {
                urlPathMatch = _urlMatcher.Match(path, template.DownstreamUrlPathTemplate);

                if (urlPathMatch.Match)
                {
                    upstreamPathUrl = template.UpstreamUrlPathTemplate;
                    break;
                }
            }

            if (!urlPathMatch.Match)
            {
                throw new Exception("BOOOM TING! no match");
            }
            
            var upstreamHostUrl = _hostUrlRepository.GetBaseUrlMap(urlPathMatch.UrlPathTemplate);

            //now map the variables from the url path to the upstream url path
            


            await _next.Invoke(context);
        }
    }
}