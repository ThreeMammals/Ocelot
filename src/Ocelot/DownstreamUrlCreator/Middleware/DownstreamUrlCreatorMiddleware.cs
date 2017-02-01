using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public class DownstreamUrlCreatorMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamPathPlaceholderReplacer _replacer;
        private readonly IOcelotLogger _logger;
        private readonly IUrlBuilder _urlBuilder;

        public DownstreamUrlCreatorMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IDownstreamPathPlaceholderReplacer replacer,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IUrlBuilder urlBuilder)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _replacer = replacer;
            _urlBuilder = urlBuilder;
            _logger = loggerFactory.CreateLogger<DownstreamUrlCreatorMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling downstream url creator middleware");

            var dsPath = _replacer
                .Replace(DownstreamRoute.ReRoute.DownstreamPathTemplate, DownstreamRoute.TemplatePlaceholderNameAndValues);

            if (dsPath.IsError)
            {
                _logger.LogDebug("IDownstreamPathPlaceholderReplacer returned an error, setting pipeline error");

                SetPipelineError(dsPath.Errors);
                return;
            }

            var dsScheme = DownstreamRoute.ReRoute.DownstreamScheme;

            //here we could have a lb factory that takes stuff or we could just get the load balancer from the reRoute?
            //returns the lb for this request
            
            //lease the next address from the lb

            //this could return the load balancer which you call next on, that gives you the host and port then you can call release in a try catch 
            //and if the call works?
            var dsHostAndPort = DownstreamRoute.ReRoute.DownstreamHostAndPort();

            var dsUrl = _urlBuilder.Build(dsPath.Data.Value, dsScheme, dsHostAndPort);

            if (dsUrl.IsError)
            {
                //todo - release the lb connection?
                _logger.LogDebug("IUrlBuilder returned an error, setting pipeline error");

                SetPipelineError(dsUrl.Errors);
                return;
            }

            _logger.LogDebug("downstream url is {downstreamUrl.Data.Value}", dsUrl.Data.Value);

            SetDownstreamUrlForThisRequest(dsUrl.Data.Value);

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            //todo - release the lb connection?

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}