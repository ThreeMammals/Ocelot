using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Cache.Middleware
{
    public class OutputCacheMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotCache<CachedResponse> _outputCache;
        private readonly ICacheKeyGenerator _cacheGenerator;

        public OutputCacheMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IOcelotCache<CachedResponse> outputCache,
            ICacheKeyGenerator cacheGenerator)
            : base(loggerFactory.CreateLogger<OutputCacheMiddleware>())
        {
            _next = next;
            _outputCache = outputCache;
            _cacheGenerator = cacheGenerator;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var downstreamRoute = httpContext.Items.DownstreamRoute();

            if (!downstreamRoute.IsCached)
            {
                await _next.Invoke(httpContext);
                return;
            }

            var downstreamRequest = httpContext.Items.DownstreamRequest();
            var downstreamUrlKey = $"{downstreamRequest.Method}-{downstreamRequest.OriginalString}";
            var downStreamRequestCacheKey = await _cacheGenerator.GenerateRequestCacheKey(downstreamRequest, downstreamRoute);

            Logger.LogDebug(() => $"Started checking cache for the '{downstreamUrlKey}' key.");
            var cached = _outputCache.Get(downStreamRequestCacheKey, downstreamRoute.CacheOptions.Region);
            if (cached != null)
            {
                Logger.LogDebug(() => $"Cache entry exists for the '{downstreamUrlKey}' key.");
                var response = CreateHttpResponseMessage(cached);
                SetHttpResponseMessageThisRequest(httpContext, response);
                Logger.LogDebug(() => $"Finished returning of cached response for the '{downstreamUrlKey}' key.");
                return;
            }

            Logger.LogDebug(() => $"No response cached for the '{downstreamUrlKey}' key.");

            await _next.Invoke(httpContext);

            if (httpContext.Items.Errors().Count > 0)
            {
                Logger.LogDebug(() => $"There was a pipeline error for the '{downstreamUrlKey}' key.");
                return;
            }

            var downstreamResponse = httpContext.Items.DownstreamResponse();
            cached = await CreateCachedResponse(downstreamResponse);

            _outputCache.Add(downStreamRequestCacheKey, cached, TimeSpan.FromSeconds(downstreamRoute.CacheOptions.TtlSeconds), downstreamRoute.CacheOptions.Region);
            Logger.LogDebug(() => $"Finished response added to cache for the '{downstreamUrlKey}' key.");
        }

        private static void SetHttpResponseMessageThisRequest(HttpContext context, DownstreamResponse response)
            => context.Items.UpsertDownstreamResponse(response);

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
