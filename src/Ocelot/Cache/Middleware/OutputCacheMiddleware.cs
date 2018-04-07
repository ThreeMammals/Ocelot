using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.IO;
using Ocelot.DownstreamRouteFinder.Middleware;

namespace Ocelot.Cache.Middleware
{
    public class OutputCacheMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IOcelotCache<CachedResponse> _outputCache;
        private readonly IRegionCreator _regionCreator;

        public OutputCacheMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IOcelotCache<CachedResponse> outputCache,
            IRegionCreator regionCreator)
                :base(loggerFactory.CreateLogger<OutputCacheMiddleware>())
        {
            _next = next;
            _outputCache = outputCache;
            _regionCreator = regionCreator;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (!context.DownstreamReRoute.IsCached)
            {
                await _next.Invoke(context);
                return;
            }

            var downstreamUrlKey = $"{context.DownstreamRequest.Method}-{context.DownstreamRequest.OriginalString}";

            Logger.LogDebug($"Started checking cache for {downstreamUrlKey}");

            var cached = _outputCache.Get(downstreamUrlKey, context.DownstreamReRoute.CacheOptions.Region);

            if (cached != null)
            {
                Logger.LogDebug($"cache entry exists for {downstreamUrlKey}");

                var response = CreateHttpResponseMessage(cached);
                SetHttpResponseMessageThisRequest(context, response);

                Logger.LogDebug($"finished returned cached response for {downstreamUrlKey}");

                return;
            }

            Logger.LogDebug($"no resonse cached for {downstreamUrlKey}");

            await _next.Invoke(context);

            if (context.IsError)
            {
                Logger.LogDebug($"there was a pipeline error for {downstreamUrlKey}");

                return;
            }

            cached = await CreateCachedResponse(context.DownstreamResponse);

            _outputCache.Add(downstreamUrlKey, cached, TimeSpan.FromSeconds(context.DownstreamReRoute.CacheOptions.TtlSeconds), context.DownstreamReRoute.CacheOptions.Region);

            Logger.LogDebug($"finished response added to cache for {downstreamUrlKey}");
        }

        private void SetHttpResponseMessageThisRequest(DownstreamContext context, HttpResponseMessage response)
        {
            context.DownstreamResponse = response;
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

            foreach (var header in cached.ContentHeaders)
            {
                response.Content.Headers.Add(header.Key, header.Value);
            }

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

            var contentHeaders = response?.Content?.Headers.ToDictionary(v => v.Key, v => v.Value);

            var cached = new CachedResponse(statusCode, headers, body, contentHeaders);
            return cached;
        }
    }
}
