using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
        {
            var builder = new StringBuilder($"{downstreamRequest.Method}-{downstreamRequest.OriginalString}");

            var cacheOptionsHeader = downstreamRoute?.CacheOptions?.Header;

            if (!string.IsNullOrEmpty(cacheOptionsHeader))
            {
                var header = downstreamRequest.Headers
                    .FirstOrDefault(r => r.Key.Equals(cacheOptionsHeader, StringComparison.OrdinalIgnoreCase))
                    .Value?.FirstOrDefault();

                if (!string.IsNullOrEmpty(header))
                {
                    builder.Append(header);
                }
            }

            if (downstreamRequest.Content != null)
            {
                var requestContentString = Task.Run(async () => await downstreamRequest.Content.ReadAsStringAsync()).Result;
                builder.Append(requestContentString);
            }

            return MD5Helper.GenerateMd5(builder.ToString());
        }
    }
}
