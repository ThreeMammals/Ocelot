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
        private readonly IOcelotLogger _logger;
        private readonly IOcelotCache<CachedResponse> _outputCache;
        private readonly IRegionCreator _regionCreator;

        public OutputCacheMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IOcelotCache<CachedResponse> outputCache,
            IRegionCreator regionCreator)
        {
            _next = next;
            _outputCache = outputCache;
            _logger = loggerFactory.CreateLogger<OutputCacheMiddleware>();
            _regionCreator = regionCreator;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (!context.DownstreamReRoute.IsCached)
            {
                await _next.Invoke(context);
                return;
            }

            var downstreamUrlKey = $"{context.DownstreamRequest.Method.Method}-{context.DownstreamRequest.RequestUri.OriginalString}";

            _logger.LogDebug("started checking cache for {downstreamUrlKey}", downstreamUrlKey);

            var cached = _outputCache.Get(downstreamUrlKey, context.DownstreamReRoute.CacheOptions.Region);

            if (cached != null)
            {
                _logger.LogDebug("cache entry exists for {downstreamUrlKey}", downstreamUrlKey);

                var response = CreateHttpResponseMessage(cached);
                SetHttpResponseMessageThisRequest(context, response);

                _logger.LogDebug("finished returned cached response for {downstreamUrlKey}", downstreamUrlKey);

                return;
            }

            _logger.LogDebug("no resonse cached for {downstreamUrlKey}", downstreamUrlKey);

            await _next.Invoke(context);

            if (context.IsError)
            {
                _logger.LogDebug("there was a pipeline error for {downstreamUrlKey}", downstreamUrlKey);

                return;
            }

            cached = await CreateCachedResponse(context.DownstreamResponse);

            _outputCache.Add(downstreamUrlKey, cached, TimeSpan.FromSeconds(context.DownstreamReRoute.CacheOptions.TtlSeconds), context.DownstreamReRoute.CacheOptions.Region);

            _logger.LogDebug("finished response added to cache for {downstreamUrlKey}", downstreamUrlKey);
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
