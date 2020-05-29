using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute)
        {
            string hashedContent = null;
            var downStreamUrlKeyBuilder = new StringBuilder($"{downstreamRequest.Method}-{downstreamRequest.OriginalString}");

            var cacheOptionsHeader = downstreamRoute?.CacheOptions?.Header;

            if (!string.IsNullOrEmpty(cacheOptionsHeader))
            {
                var header = downstreamRequest.Headers.FirstOrDefault(r =>
                        r.Key.Equals(cacheOptionsHeader, StringComparison.OrdinalIgnoreCase))
                    .Value?.FirstOrDefault();

                if (!string.IsNullOrEmpty(header))
                {
                    downStreamUrlKeyBuilder.Append(header);
                }
            }

            if (downstreamRequest.Content != null)
            {
                var requestContentString = Task.Run(async () => await downstreamRequest.Content.ReadAsStringAsync()).Result;
                downStreamUrlKeyBuilder.Append(requestContentString);
            }

            var hashedContent = MD5Helper.GenerateMd5(downStreamUrlKeyBuilder.ToString());
            return hashedContent;
        }
    }
}
