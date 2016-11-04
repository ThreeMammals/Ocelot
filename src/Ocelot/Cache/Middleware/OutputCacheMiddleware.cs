using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.Cache.Middleware
{
    public class OutputCacheMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IOcelotCache<HttpResponseMessage> _outputCache;

        public OutputCacheMiddleware(RequestDelegate next, 
            ILoggerFactory loggerFactory,
            IRequestScopedDataRepository scopedDataRepository,
            IOcelotCache<HttpResponseMessage> outputCache)
            :base(scopedDataRepository)
        {
            _next = next;
            _outputCache = outputCache;
            _logger = loggerFactory.CreateLogger<OutputCacheMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamUrlKey = DownstreamUrl;

            if (!DownstreamRoute.ReRoute.IsCached)
            {
                await _next.Invoke(context);
                return;
            }

            _logger.LogDebug("OutputCacheMiddleware.Invoke stared checking cache for downstreamUrlKey", downstreamUrlKey);
  
            var cached = _outputCache.Get(downstreamUrlKey);

            if (cached != null)
            {
                SetHttpResponseMessageThisRequest(cached);

                _logger.LogDebug("OutputCacheMiddleware.Invoke finished returned cached response for downstreamUrlKey", downstreamUrlKey);

                return;
            }

            _logger.LogDebug("OutputCacheMiddleware.Invoke no resonse cached for downstreamUrlKey", downstreamUrlKey);

            await _next.Invoke(context);

            if (PipelineError())
            {
                _logger.LogDebug("OutputCacheMiddleware.Invoke there was a pipeline error for downstreamUrlKey", downstreamUrlKey);

                return;
            }

            var response = HttpResponseMessage;

            _outputCache.Add(downstreamUrlKey, response, TimeSpan.FromSeconds(DownstreamRoute.ReRoute.FileCacheOptions.TtlSeconds));

            _logger.LogDebug("OutputCacheMiddleware.Invoke finished response added to cache for downstreamUrlKey", downstreamUrlKey);
        }
    }
}
