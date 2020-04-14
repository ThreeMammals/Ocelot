namespace Ocelot.Cache.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;

    public class OutputCacheMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotCache<CachedResponse> _outputCache;
        private readonly ICacheKeyGenerator _cacheGenerator;

        public OutputCacheMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IOcelotCache<CachedResponse> outputCache,
            ICacheKeyGenerator cacheGenerator,
            IRequestScopedDataRepository repo)
                : base(loggerFactory.CreateLogger<OutputCacheMiddleware>(), repo)
        {
            _next = next;
            _outputCache = outputCache;
            _cacheGenerator = cacheGenerator;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!DownstreamContext.Data.DownstreamReRoute.IsCached)
            {
                await _next.Invoke(httpContext);
                return;
            }

            var downstreamUrlKey = $"{DownstreamContext.Data.DownstreamRequest.Method}-{DownstreamContext.Data.DownstreamRequest.OriginalString}";
            string downStreamRequestCacheKey = _cacheGenerator.GenerateRequestCacheKey(DownstreamContext.Data);

            Logger.LogDebug($"Started checking cache for {downstreamUrlKey}");

            var cached = _outputCache.Get(downStreamRequestCacheKey, DownstreamContext.Data.DownstreamReRoute.CacheOptions.Region);

            if (cached != null)
            {
                Logger.LogDebug($"cache entry exists for {downstreamUrlKey}");

                var response = CreateHttpResponseMessage(cached);
                SetHttpResponseMessageThisRequest(DownstreamContext.Data, response);

                Logger.LogDebug($"finished returned cached response for {downstreamUrlKey}");

                return;
            }

            Logger.LogDebug($"no resonse cached for {downstreamUrlKey}");

            await _next.Invoke(httpContext);

            if (DownstreamContext.Data.IsError)
            {
                Logger.LogDebug($"there was a pipeline error for {downstreamUrlKey}");

                return;
            }

            cached = await CreateCachedResponse(DownstreamContext.Data.DownstreamResponse);

            _outputCache.Add(downStreamRequestCacheKey, cached, TimeSpan.FromSeconds(DownstreamContext.Data.DownstreamReRoute.CacheOptions.TtlSeconds), DownstreamContext.Data.DownstreamReRoute.CacheOptions.Region);

            Logger.LogDebug($"finished response added to cache for {downstreamUrlKey}");
        }

        private void SetHttpResponseMessageThisRequest(DownstreamContext context,
                                                       DownstreamResponse response)
        {
            context.DownstreamResponse = response;
        }

        internal DownstreamResponse CreateHttpResponseMessage(CachedResponse cached)
        {
            if (cached == null)
            {
                return null;
            }

            var content = new MemoryStream(Convert.FromBase64String(cached.Body));

            var streamContent = new StreamContent(content);

            foreach (var header in cached.ContentHeaders)
            {
                streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return new DownstreamResponse(streamContent, cached.StatusCode, cached.Headers.ToList(), cached.ReasonPhrase);
        }

        internal async Task<CachedResponse> CreateCachedResponse(DownstreamResponse response)
        {
            if (response == null)
            {
                return null;
            }

            var statusCode = response.StatusCode;
            var headers = response.Headers.ToDictionary(v => v.Key, v => v.Values);
            string body = null;

            if (response.Content != null)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                body = Convert.ToBase64String(content);
            }

            var contentHeaders = response?.Content?.Headers.ToDictionary(v => v.Key, v => v.Value);

            var cached = new CachedResponse(statusCode, headers, body, contentHeaders, response.ReasonPhrase);
            return cached;
        }
    }
}
