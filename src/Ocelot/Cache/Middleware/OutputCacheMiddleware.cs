using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.IO;

namespace Ocelot.Cache.Middleware
{
    public class OutputCacheMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IOcelotCache<CachedResponse> _outputCache;
        private readonly IRegionCreator _regionCreator;

        public OutputCacheMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository scopedDataRepository,
            IOcelotCache<CachedResponse> outputCache,
            IRegionCreator regionCreator)
            : base(scopedDataRepository)
        {
            _next = next;
            _outputCache = outputCache;
            _logger = loggerFactory.CreateLogger<OutputCacheMiddleware>();
            _regionCreator = regionCreator;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!DownstreamRoute.ReRoute.IsCached)
            {
                await _next.Invoke(context);
                return;
            }

            var downstreamUrlKey = $"{DownstreamRequest.Method.Method}-{DownstreamRequest.RequestUri.OriginalString}";

            _logger.LogDebug("started checking cache for {downstreamUrlKey}", downstreamUrlKey);

            var cached = _outputCache.Get(downstreamUrlKey, DownstreamRoute.ReRoute.CacheOptions.Region);

            if (cached != null)
            {
                _logger.LogDebug("cache entry exists for {downstreamUrlKey}", downstreamUrlKey);

                var response = CreateHttpResponseMessage(cached);
                SetHttpResponseMessageThisRequest(response);

                _logger.LogDebug("finished returned cached response for {downstreamUrlKey}", downstreamUrlKey);

                return;
            }

            _logger.LogDebug("no resonse cached for {downstreamUrlKey}", downstreamUrlKey);

            await _next.Invoke(context);

            if (PipelineError)
            {
                _logger.LogDebug("there was a pipeline error for {downstreamUrlKey}", downstreamUrlKey);

                return;
            }

            cached = await CreateCachedResponse(HttpResponseMessage);

            _outputCache.Add(downstreamUrlKey, cached, TimeSpan.FromSeconds(DownstreamRoute.ReRoute.CacheOptions.TtlSeconds), DownstreamRoute.ReRoute.CacheOptions.Region);

            _logger.LogDebug("finished response added to cache for {downstreamUrlKey}", downstreamUrlKey);
        }

        internal HttpResponseMessage CreateHttpResponseMessage(CachedResponse cached)
        {
            if (cached == null)
            {
                return null;
            }

            var response = new HttpResponseMessage(cached.StatusCode);
            foreach (var header in cached.Headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }
            var content = new MemoryStream(Convert.FromBase64String(cached.Body));
            response.Content = new StreamContent(content);

            return response;
        }

        internal async Task<CachedResponse> CreateCachedResponse(HttpResponseMessage response)
        {
            if (response == null)
            {
                return null;
            }

            var statusCode = response.StatusCode;
            var headers = response.Headers.ToDictionary(v => v.Key, v => v.Value);
            string body = null;

            if (response.Content != null)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                body = Convert.ToBase64String(content);
            }

            var cached = new CachedResponse(statusCode, headers, body);
            return cached;
        }
    }
}
