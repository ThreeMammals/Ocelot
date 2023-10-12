using Ocelot.Configuration;
using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public async ValueTask<string> GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
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

            if (downstreamRequest.Content == null) return MD5Helper.GenerateMd5(builder.ToString());

            var requestContentString = await downstreamRequest.Content.ReadAsStringAsync();
            builder.Append(requestContentString);

            return MD5Helper.GenerateMd5(builder.ToString());
        }
    }
}
